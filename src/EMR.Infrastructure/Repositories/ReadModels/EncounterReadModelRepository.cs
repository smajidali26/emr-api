using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories.ReadModels;

/// <summary>
/// Specialized repository for encounter read models
/// </summary>
public class EncounterReadModelRepository : ReadModelRepository<EncounterListReadModel>, IEncounterReadModelRepository
{
    public EncounterReadModelRepository(ReadDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<EncounterListReadModel>> GetByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.PatientId == patientId)
            .OrderByDescending(e => e.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EncounterListReadModel>> GetByProviderAsync(
        Guid providerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(e => e.ProviderId == providerId);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ScheduledAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ScheduledAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(e => e.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EncounterListReadModel>> GetByStatusAsync(
        string status,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(e => e.Status == status);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ScheduledAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ScheduledAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(e => e.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EncounterListReadModel>> GetByDepartmentAsync(
        string department,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(e => e.Department == department);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.ScheduledAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.ScheduledAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(e => e.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EncounterListReadModel?> GetByEncounterNumberAsync(
        string encounterNumber,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EncounterNumber == encounterNumber, cancellationToken);
    }

    public async Task<IReadOnlyList<EncounterListReadModel>> GetActiveEncountersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == "In Progress" || e.Status == "Scheduled")
            .OrderBy(e => e.ScheduledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EncounterListReadModel>> GetScheduledForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _dbSet
            .AsNoTracking()
            .Where(e => e.ScheduledAt >= startOfDay && e.ScheduledAt < endOfDay)
            .OrderBy(e => e.ScheduledAt)
            .ToListAsync(cancellationToken);
    }
}
