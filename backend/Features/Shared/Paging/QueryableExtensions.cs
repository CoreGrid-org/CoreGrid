using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CoreGrid.Api.Features.Shared.Paging;

// Provides reusable pagination for queryable data.
public static class QueryableExtensions
{
    public static async Task<PagedResult<TResult>> ToPagedResultAsync<TSource, TResult>(
        this IQueryable<TSource> query,
        PagedQuery pagedQuery,
        Expression<Func<TSource, TResult>> projection,
        CancellationToken cancellationToken,
        int maxPageSize = PagedQuery.MaxPageSize)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var page = pagedQuery.ClampedPage;
        var pageSize = pagedQuery.ClampedPageSize(maxPageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<TResult>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}
