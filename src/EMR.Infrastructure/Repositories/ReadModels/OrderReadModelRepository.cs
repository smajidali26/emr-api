using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories.ReadModels;

/// <summary>
/// Specialized repository for order read models (worklist)
/// </summary>
public class OrderReadModelRepository : ReadModelRepository<ActiveOrdersReadModel>, IOrderReadModelRepository
{
    public OrderReadModelRepository(ReadDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetActiveByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.PatientId == patientId &&
                       (o.Status == "Pending" || o.Status == "In Progress"))
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.OrderedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetByEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.EncounterId == encounterId)
            .OrderByDescending(o => o.OrderedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetByTypeAndStatusAsync(
        string orderType,
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.OrderType == orderType && o.Status == status)
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.OrderedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetAssignedToAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.AssignedToId == userId &&
                       (o.Status == "Pending" || o.Status == "In Progress"))
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.ScheduledFor ?? o.OrderedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetByDepartmentAsync(
        string department,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(o => o.PerformingDepartment == department);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }

        return await query
            .OrderByDescending(o => o.Priority)
            .ThenBy(o => o.OrderedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetUrgentOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => (o.Priority == "STAT" || o.Priority == "Urgent") &&
                       (o.Status == "Pending" || o.Status == "In Progress"))
            .OrderBy(o => o.Priority == "STAT" ? 1 : 2)
            .ThenBy(o => o.OrderedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetOverdueOrdersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.IsOverdue && (o.Status == "Pending" || o.Status == "In Progress"))
            .OrderByDescending(o => o.AgeInHours)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetRequiringAuthorizationAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.RequiresAuthorization &&
                       (o.AuthorizationStatus == null || o.AuthorizationStatus == "Pending") &&
                       (o.Status == "Pending" || o.Status == "In Progress"))
            .OrderBy(o => o.OrderedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ActiveOrdersReadModel>> GetCriticalResultsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(o => o.IsCritical && o.HasResults)
            .OrderByDescending(o => o.CompletedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ActiveOrdersReadModel?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }
}
