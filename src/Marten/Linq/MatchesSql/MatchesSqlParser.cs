using System.Linq.Expressions;
using System.Reflection;
using JasperFx.Core.Reflection;
using Marten.Linq.Members;
using Marten.Linq.Parsing;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.MatchesSql;

public class MatchesSqlParser: IMethodCallParser
{
    private static readonly MethodInfo _sqlMethod =
        typeof(MatchesSqlExtensions).GetMethod(nameof(MatchesSqlExtensions.MatchesSql),
            new[] { typeof(object), typeof(string), typeof(object[]) });

    private static readonly MethodInfo _fragmentMethod =
        typeof(MatchesSqlExtensions).GetMethod(nameof(MatchesSqlExtensions.MatchesSql),
            new[] { typeof(object), typeof(ISqlFragment) });

    public bool Matches(MethodCallExpression expression)
    {
        return Equals(expression.Method, _sqlMethod) || Equals(expression.Method, _fragmentMethod);
    }

    public ISqlFragment Parse(IQueryableMemberCollection memberCollection, IReadOnlyStoreOptions options,
        MethodCallExpression expression)
    {
        if (expression.Method.Equals(_sqlMethod))
        {
            return new WhereFragment(expression.Arguments[1].Value().As<string>(),
                expression.Arguments[2].Value().As<object[]>());
        }

        if (expression.Method.Equals(_fragmentMethod))
        {
            return expression.Arguments[1].Value() as ISqlFragment;
        }

        return null;
    }
}
