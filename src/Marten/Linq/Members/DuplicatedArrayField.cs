using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JasperFx.Core.Reflection;
using Marten.Exceptions;
using Marten.Internal;
using Marten.Linq.Parsing;
using Marten.Linq.SqlGeneration;
using Marten.Linq.SqlGeneration.Filters;
using Weasel.Core;
using Weasel.Postgresql;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.Members;

internal class DuplicatedArrayField: DuplicatedField, ICollectionMember, IQueryableMemberCollection
{
    public DuplicatedArrayField(EnumStorage enumStorage, QueryableMember innerMember, bool useTimestampWithoutTimeZoneForDateTime = true, bool notNull = false) : base(enumStorage, innerMember, useTimestampWithoutTimeZoneForDateTime, notNull)
    {
        if (innerMember is not ValueCollectionMember)
            throw new ArgumentOutOfRangeException(nameof(innerMember),
                $"Only collections of simple types can be duplicated. Actual element type: {innerMember.MemberType.FullNameInCode()}");

        ElementType = InnerMember.As<ICollectionMember>().ElementType;
        ExplodeLocator =
            $"UNNEST({ArrayLocator})";

        _wholeDataMember = new WholeDataMember(ElementType);

        var innerPgType = PostgresqlProvider.Instance.GetDatabaseType(ElementType, EnumStorage.AsInteger);
        var pgType = PostgresqlProvider.Instance.HasTypeMapping(ElementType) ? innerPgType + "[]" : "jsonb";
        ElementMember = new SimpleElementMember(ElementType, pgType);

        _count = new CollectionLengthMember(this);
    }

    private readonly WholeDataMember _wholeDataMember;
    private readonly CollectionLengthMember _count;
    public Type ElementType { get; }
    public string ExplodeLocator { get; }
    public string ArrayLocator => TypedLocator;
    public IQueryableMember ElementMember { get; }

    public SelectorStatement BuildChildStatement(CollectionUsage collectionUsage, IMartenSession session,
        SelectorStatement parentStatement)
    {
        var statement = ElementType == typeof(string)
            ? new ScalarSelectManyStringStatement(parentStatement)
            : typeof(ScalarSelectManyStatement<>).CloseAndBuildAs<SelectorStatement>(parentStatement,
                session.Serializer, ElementType);

        collectionUsage.ProcessSingleValueModeIfAny(statement);

        return statement;
    }

    public ISelectClause BuildSelectClauseForExplosion(string fromObject)
    {
        var selection = $"jsonb_array_elements_text({JSONBLocator})";

        return typeof(DataSelectClause<>).CloseAndBuildAs<ISelectClause>(fromObject, selection,
            ElementType);
    }

    public ISqlFragment ParseWhereForAny(Expression body, IReadOnlyStoreOptions options)
    {
        var whereClause = new ChildCollectionWhereClause();
        var parser = new WhereClauseParser((StoreOptions)options, this, whereClause);
        parser.Visit(body);

        return whereClause.CompileFragment(this, options.Serializer());
    }

    public IComparableMember ParseComparableForCount(Expression body)
    {
        throw new BadLinqExpressionException(
            "Marten does not (yet) support Linq filters within the Count() expression of a scalar value collection");
    }

    public ISqlFragment ParseWhereForAll(MethodCallExpression body, IReadOnlyStoreOptions options)
    {
        var constant = body.Arguments[1].ReduceToConstant();

        if (constant.Value() == null) return new AllValuesAreNullFilter(this);

        return new AllValuesEqualFilter(constant, this);
    }

    public ISqlFragment ParseWhereForContains(MethodCallExpression body, IReadOnlyStoreOptions options)
    {
        return new WhereFragment($"? = ANY({TypedLocator})", body.Arguments.Last().Value());
    }

    public IQueryableMember FindMember(MemberInfo member)
    {
        if (member.Name == "Count" || member.Name == "Length")
        {
            return _count;
        }

        return _wholeDataMember;
    }

    public void ReplaceMember(MemberInfo member, IQueryableMember queryableMember)
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<IQueryableMember> GetEnumerator()
    {
        throw new NotSupportedException();
    }
}
