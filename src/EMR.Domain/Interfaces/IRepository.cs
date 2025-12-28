using EMR.Domain.Common;
using System.Linq.Expressions;

namespace EMR.Domain.Interfaces;

/// <summary>
/// Generic repository interface for data access operations
/// </summary>
/// <typeparam name="TEntity">Entity type that inherits from BaseEntity</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>
    /// Get entity by ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entity by ID including specified related entities
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Get all entities
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entities matching the predicate
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entities matching the predicate with includes
    /// </summary>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Get first entity matching the predicate or null
    /// </summary>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new entity
    /// </summary>
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add multiple entities
    /// </summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing entity
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Update multiple entities
    /// </summary>
    void UpdateRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Remove an entity (soft delete if supported)
    /// </summary>
    void Remove(TEntity entity);

    /// <summary>
    /// Remove multiple entities (soft delete if supported)
    /// </summary>
    void RemoveRange(IEnumerable<TEntity> entities);

    /// <summary>
    /// Check if any entity matches the predicate
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count entities matching the predicate
    /// </summary>
    Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paged results
    /// </summary>
    Task<(IReadOnlyList<TEntity> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}
