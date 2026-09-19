using System.Linq.Expressions;

namespace CoreGrid.Api.Features.Shared.Scoping;

public static class DepartmentScopeExtensions
{
    // A restricted caller with no department assigned must see nothing,
    // not everything — an unrestricted-looking "no filter" here would be
    // exactly the FR-086 gap this scope exists to close.
    public static IQueryable<T> ApplyScope<T>(
        this IQueryable<T> query,
        DepartmentScope scope,
        Expression<Func<T, Guid?>> departmentIdSelector)
    {
        if (!scope.IsRestricted)
        {
            return query;
        }

        if (scope.DepartmentId is not { } departmentId)
        {
            return query.Where(_ => false);
        }

        var parameter = departmentIdSelector.Parameters[0];
        var comparison = Expression.Equal(departmentIdSelector.Body, Expression.Constant(departmentId, typeof(Guid?)));
        var predicate = Expression.Lambda<Func<T, bool>>(comparison, parameter);

        return query.Where(predicate);
    }
}
