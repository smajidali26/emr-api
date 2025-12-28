using EMR.Domain.ReadModels;
using System.Linq.Expressions;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Generic repository interface for read model operations
/// Optimized for query performance
/// </summary>
/// <typeparam name="TReadModel">The read model type</typeparam>
public interface IReadModelRepository<TReadModel> where TReadModel : BaseReadModel
{
    /// <summary>
    /// Gets a read model by ID
    /// </summary>
    Task<TReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all read models matching a predicate
    /// </summary>
    Task<IReadOnlyList<TReadModel>> GetAsync(
        Expression<Func<TReadModel, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paged read models matching a predicate
    /// </summary>
    Task<(IReadOnlyList<TReadModel> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TReadModel, bool>>? predicate = null,
        Expression<Func<TReadModel, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any read model matches the predicate
    /// </summary>
    Task<bool> AnyAsync(
        Expression<Func<TReadModel, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts read models matching a predicate
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TReadModel, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a read model
    /// </summary>
    Task UpsertAsync(TReadModel readModel, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates multiple read models
    /// </summary>
    Task UpsertRangeAsync(IEnumerable<TReadModel> readModels, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a read model by ID
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple read models
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all read models matching a predicate
    /// </summary>
    Task DeleteWhereAsync(
        Expression<Func<TReadModel, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all read models (use with caution)
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}
