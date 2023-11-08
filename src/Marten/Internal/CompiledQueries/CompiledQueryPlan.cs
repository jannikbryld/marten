using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JasperFx.Core;
using JasperFx.Core.Reflection;
using Marten.Events.CodeGeneration;
using Marten.Exceptions;
using Marten.Linq;
using Marten.Linq.Includes;
using Marten.Linq.QueryHandlers;
using Marten.Linq.SqlGeneration;
using Marten.Schema.Arguments;
using Npgsql;

namespace Marten.Internal.CompiledQueries;

internal class CompiledQueryPlan
{
    public const string ParameterPlaceholder = "^";

    public CompiledQueryPlan(Type queryType, Type outputType)
    {
        QueryType = queryType;
        OutputType = outputType;
    }

    public Type QueryType { get; }
    public Type OutputType { get; }

    public IList<MemberInfo> InvalidMembers { get; } = new List<MemberInfo>();

    public IList<IQueryMember> Parameters { get; } = new List<IQueryMember>();


    public NpgsqlCommand Command { get; set; }

    public IQueryHandler HandlerPrototype { get; set; }

    public MemberInfo StatisticsMember { get; set; }

    public IList<MemberInfo> IncludeMembers { get; } = new List<MemberInfo>();

    internal IList<IIncludePlan> IncludePlans { get; } = new List<IIncludePlan>();

    public void FindMembers()
    {
        foreach (var member in findMembers())
        {
            var memberType = member.GetRawMemberType();
            if (memberType == typeof(QueryStatistics))
            {
                StatisticsMember = member;
            }

            else if (memberType.Closes(typeof(IDictionary<,>)))
            {
                IncludeMembers.Add(member);
            }
            else if (memberType.Closes(typeof(Action<>)))
            {
                IncludeMembers.Add(member);
            }
            else if (memberType.Closes(typeof(IList<>)))
            {
                IncludeMembers.Add(member);
            }
            else if (memberType.IsNullable())
            {
                InvalidMembers.Add(member);
            }
            else if (QueryCompiler.Finders.All(x => !x.Matches(memberType)))
            {
                InvalidMembers.Add(member);
            }
            else if (member is PropertyInfo)
            {
                var queryMember = typeof(PropertyQueryMember<>).CloseAndBuildAs<IQueryMember>(member, memberType);
                Parameters.Add(queryMember);
            }
            else if (member is FieldInfo)
            {
                var queryMember = typeof(FieldQueryMember<>).CloseAndBuildAs<IQueryMember>(member, memberType);
                Parameters.Add(queryMember);
            }
        }
    }

    private IEnumerable<MemberInfo> findMembers()
    {
        foreach (var field in QueryType.GetFields(BindingFlags.Instance | BindingFlags.Public)
                     .Where(x => !x.HasAttribute<MartenIgnoreAttribute>())) yield return field;

        foreach (var property in QueryType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(x => !x.HasAttribute<MartenIgnoreAttribute>())) yield return property;
    }

    public string CorrectedCommandText()
    {
        var text = Command.CommandText;

        for (var i = Command.Parameters.Count - 1; i >= 0; i--)
        {
            var parameterName = Command.Parameters[i].ParameterName;
            if (parameterName == TenantIdArgument.ArgName)
            {
                continue;
            }

            text = text.Replace(":" + parameterName, ParameterPlaceholder);
        }

        return text;
    }


    public QueryStatistics GetStatisticsIfAny(object query)
    {
        if (StatisticsMember is PropertyInfo p)
        {
            return (QueryStatistics)p.GetValue(query) ?? new QueryStatistics();
        }

        if (StatisticsMember is FieldInfo f)
        {
            return (QueryStatistics)f.GetValue(query) ?? new QueryStatistics();
        }

        return null;
    }

    public ICompiledQuery<TDoc, TOut> CreateQueryTemplate<TDoc, TOut>(ICompiledQuery<TDoc, TOut> query)
    {
        foreach (var parameter in Parameters) parameter.StoreValue(query);

        if (!(query is IQueryPlanning) && AreAllMemberValuesUnique(query))
        {
            return query;
        }

        try
        {
            return (ICompiledQuery<TDoc, TOut>)TryCreateUniqueTemplate(query.GetType());
        }
        catch (Exception e)
        {
            throw new InvalidCompiledQueryException("Unable to create a Compiled Query template", e);
        }
    }

    private bool AreAllMemberValuesUnique(object query)
    {
        return QueryCompiler.Finders.All(x => x.AreValuesUnique(query, this));
    }

    public object TryCreateUniqueTemplate(Type type)
    {
        var constructor = type.GetConstructors().MaxBy(x => x.GetParameters().Count());


        if (constructor == null)
        {
            throw new InvalidOperationException("Cannot find a suitable constructor for query planning for type " +
                                                type.FullNameInCode());
        }

        var valueSource = new UniqueValueSource();

        var ctorArgs = valueSource.ArgsFor(constructor);
        var query = Activator.CreateInstance(type, ctorArgs);
        if (query is IQueryPlanning planning)
        {
            planning.SetUniqueValuesForQueryPlanning();
            foreach (var member in Parameters) member.StoreValue(query);
        }

        if (AreAllMemberValuesUnique(query))
        {
            return query;
        }

        foreach (var queryMember in Parameters) queryMember.TryWriteValue(valueSource, query);

        if (AreAllMemberValuesUnique(query))
        {
            return query;
        }

        throw new InvalidCompiledQueryException("Marten is unable to create a compiled query plan for type " +
                                                type.FullNameInCode());
    }

    public void ReadCommand(NpgsqlCommand command, Statement statement, StoreOptions storeOptions)
    {
        Command = command;

        var filters = statement.AllFilters().OfType<ICompiledQueryAwareFilter>().ToArray();

        var parameters = command.Parameters.ToList();
        parameters.RemoveAll(x => x.ParameterName == TenantIdArgument.ArgName);
        foreach (var parameter in Parameters)
        {
            parameter.TryMatch(parameters, filters, storeOptions);
        }

        var missing = Parameters.Where(x => !x.Usages.Any());
        if (missing.Any())
        {
            throw new InvalidCompiledQueryException(
                $"Unable to match compiled query member(s) {missing.Select(x => x.Member.Name).Join(", ")} with a command parameter");
        }
    }
}
