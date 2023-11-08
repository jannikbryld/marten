using System;
using System.Linq;
using JasperFx.Core;
using Marten.Internal;
using Marten.Linq.QueryHandlers;
using Marten.Linq.Selectors;
using Weasel.Postgresql;
using Weasel.Postgresql.SqlGeneration;

namespace Marten.Linq.SqlGeneration;

internal class StatsSelectClause<T>: ISelectClause
{
    private QueryStatistics _statistics;

    public StatsSelectClause(ISelectClause inner, QueryStatistics statistics)
    {
        Inner = inner;
        _statistics = statistics;
    }

    public ISelectClause Inner { get; }

    bool ISqlFragment.Contains(string sqlText)
    {
        return false;
    }

    public Type SelectedType => Inner.SelectedType;

    public string FromObject => Inner.FromObject;

    public void Apply(CommandBuilder sql)
    {
        sql.Append("select ");
        sql.Append(Inner.SelectFields().Join(", "));
        sql.Append(", ");
        sql.Append(LinqConstants.StatsColumn);
        sql.Append(" from ");
        sql.Append(Inner.FromObject);
        sql.Append(" as d");
    }

    public string[] SelectFields()
    {
        return Inner.SelectFields().Concat(new[] { LinqConstants.StatsColumn }).ToArray();
    }

    public ISelector BuildSelector(IMartenSession session)
    {
        return Inner.BuildSelector(session);
    }

    public IQueryHandler<TResult> BuildHandler<TResult>(IMartenSession session, ISqlFragment topStatement,
        ISqlFragment currentStatement)
    {
        var selector = (ISelector<T>)Inner.BuildSelector(session);

        var handler =
            new ListWithStatsQueryHandler<T>(Inner.SelectFields().Length, topStatement, selector, _statistics);

        if (handler is IQueryHandler<TResult> h)
        {
            return h;
        }

        throw new NotSupportedException("QueryStatistics queries are only supported for enumerable results");
    }

    public ISelectClause UseStatistics(QueryStatistics statistics)
    {
        _statistics = statistics;
        return this;
    }
}
