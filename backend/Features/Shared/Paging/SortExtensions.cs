using System.Linq.Expressions;

namespace CoreGrid.Api.Features.Shared.Paging;

// Applies sorting using the allowed sort fields.
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
