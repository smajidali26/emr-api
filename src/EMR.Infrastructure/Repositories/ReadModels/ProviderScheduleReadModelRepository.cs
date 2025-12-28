using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories.ReadModels;

/// <summary>
/// Specialized repository for provider schedule read models
/// </summary>
public class ProviderScheduleReadModelRepository : ReadModelRepository<ProviderScheduleReadModel>, IProviderScheduleReadModelRepository
{
    public ProviderScheduleReadModelRepository(ReadDbContext context) : base(context)
    {
    }

    public async Task<ProviderScheduleReadModel?> GetByProviderAndDateAsync(
        Guid providerId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;

        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.ProviderId == providerId && s.ScheduleDate == scheduleDate,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderScheduleReadModel>> GetByProviderRangeAsync(
        Guid providerId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date;

        return await _dbSet
            .AsNoTracking()
            .Where(s => s.ProviderId == providerId &&
                       s.ScheduleDate >= startDate &&
                       s.ScheduleDate <= endDate)
            .OrderBy(s => s.ScheduleDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderScheduleReadModel>> GetByDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;

        return await _dbSet
            .AsNoTracking()
            .Where(s => s.ScheduleDate == scheduleDate)
            .OrderBy(s => s.Department)
            .ThenBy(s => s.ProviderName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderScheduleReadModel>> GetByDepartmentAndDateAsync(
        string department,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;

        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Department == department && s.ScheduleDate == scheduleDate)
            .OrderBy(s => s.ProviderName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderScheduleReadModel>> GetAvailableBySpecialtyAsync(
        string specialty,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;

        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Specialty == specialty &&
                       s.ScheduleDate == scheduleDate &&
                       s.AvailableSlotsCount > 0)
            .OrderByDescending(s => s.AvailableSlotsCount)
            .ThenBy(s => s.ProviderName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderScheduleReadModel>> GetOnCallProvidersAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;

        return await _dbSet
            .AsNoTracking()
            .Where(s => s.ScheduleDate == scheduleDate && s.IsOnCall)
            .OrderBy(s => s.Specialty)
            .ThenBy(s => s.ProviderName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderScheduleReadModel>> GetVirtualAvailabilityAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var startDate = fromDate.Date;
        var endDate = toDate.Date;

        return await _dbSet
            .AsNoTracking()
            .Where(s => s.ScheduleDate >= startDate &&
                       s.ScheduleDate <= endDate &&
                       s.IsVirtualAvailable &&
                       s.AvailableSlotsCount > 0)
            .OrderBy(s => s.ScheduleDate)
            .ThenBy(s => s.Specialty)
            .ThenBy(s => s.ProviderName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderScheduleReadModel>> GetAcceptingNewPatientsAsync(
        DateTime date,
        string? specialty = null,
        CancellationToken cancellationToken = default)
    {
        var scheduleDate = date.Date;

        var query = _dbSet
            .AsNoTracking()
            .Where(s => s.ScheduleDate == scheduleDate &&
                       s.AcceptingNewPatients &&
                       s.AvailableSlotsCount > 0);

        if (!string.IsNullOrEmpty(specialty))
        {
            query = query.Where(s => s.Specialty == specialty);
        }

        return await query
            .OrderBy(s => s.Specialty)
            .ThenBy(s => s.ProviderName)
            .ToListAsync(cancellationToken);
    }
}
