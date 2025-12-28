using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace EMR.Infrastructure.Repositories.ReadModels;

/// <summary>
/// Generic repository implementation for read models
/// Optimized for query performance with no tracking
/// </summary>
/// <typeparam name="TReadModel">The read model type</typeparam>
public class ReadModelRepository<TReadModel> : IReadModelRepository<TReadModel>
    where TReadModel : BaseReadModel
{
    protected readonly ReadDbContext _context;
    protected readonly DbSet<TReadModel> _dbSet;

    public ReadModelRepository(ReadDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<TReadModel>();
    }

    public virtual async Task<TReadModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TReadModel>> GetAsync(
        Expression<Func<TReadModel, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public virtual async Task<(IReadOnlyList<TReadModel> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<TReadModel, bool>>? predicate = null,
        Expression<Func<TReadModel, object>>? orderBy = null,
        bool ascending = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy != null)
        {
            query = ascending
                ? query.OrderBy(orderBy)
                : query.OrderByDescending(orderBy);
        }

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TReadModel, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TReadModel, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync(cancellationToken);
    }

    public virtual async Task UpsertAsync(TReadModel readModel, CancellationToken cancellationToken = default)
    {
        if (readModel == null)
        {
            throw new ArgumentNullException(nameof(readModel));
        }

        var existing = await _dbSet.FindAsync(new object[] { readModel.Id }, cancellationToken);

        if (existing != null)
        {
            // Update existing
            _context.Entry(existing).CurrentValues.SetValues(readModel);

            // Handle owned entities for complex read models
            _context.Entry(existing).State = EntityState.Modified;
        }
        else
        {
            // Add new
            await _dbSet.AddAsync(readModel, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpsertRangeAsync(
        IEnumerable<TReadModel> readModels,
        CancellationToken cancellationToken = default)
    {
        if (readModels == null)
        {
            throw new ArgumentNullException(nameof(readModels));
        }

        var modelsList = readModels.ToList();
        if (!modelsList.Any())
        {
            return;
        }

        var ids = modelsList.Select(m => m.Id).ToList();
        var existingModels = await _dbSet
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var existingIds = existingModels.Select(e => e.Id).ToHashSet();

        foreach (var model in modelsList)
        {
            if (existingIds.Contains(model.Id))
            {
                var existing = existingModels.First(e => e.Id == model.Id);
                _context.Entry(existing).CurrentValues.SetValues(model);
            }
            else
            {
                await _dbSet.AddAsync(model, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);

        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task DeleteRangeAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids == null)
        {
            throw new ArgumentNullException(nameof(ids));
        }

        var idsList = ids.ToList();
        if (!idsList.Any())
        {
            return;
        }

        var entitiesToDelete = await _dbSet
            .Where(e => idsList.Contains(e.Id))
            .ToListAsync(cancellationToken);

        if (entitiesToDelete.Any())
        {
            _dbSet.RemoveRange(entitiesToDelete);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task DeleteWhereAsync(
        Expression<Func<TReadModel, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var entitiesToDelete = await _dbSet
            .Where(predicate)
            .ToListAsync(cancellationToken);

        if (entitiesToDelete.Any())
        {
            _dbSet.RemoveRange(entitiesToDelete);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public virtual async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        var allEntities = await _dbSet.ToListAsync(cancellationToken);

        if (allEntities.Any())
        {
            _dbSet.RemoveRange(allEntities);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
