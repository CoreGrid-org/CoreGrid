using System.Linq.Expressions;

namespace CoreGrid.Api.Features.Shared.Paging;

// Applies a client-requested sort against a fixed, feature-declared
// allowlist of sortable columns — never against an arbitrary property name
// pulled straight off the query string, which would be an easy way to
// force a full table scan or reach a column that shouldn't be exposed.
public static class SortExtensions
{
    public static IOrderedQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        PagedQuery pagedQuery,
        IReadOnlyDictionary<string, Expression<Func<T, object?>>> sortMap,
        string defaultSortKey)
    {
        var key = !string.IsNullOrWhiteSpace(pagedQuery.SortBy) && sortMap.ContainsKey(pagedQuery.SortBy!)
            ? pagedQuery.SortBy!
            : defaultSortKey;

        var selector = sortMap[key];

        return pagedQuery.IsDescending
            ? query.OrderByDescending(selector)
            : query.OrderBy(selector);
    }
}
