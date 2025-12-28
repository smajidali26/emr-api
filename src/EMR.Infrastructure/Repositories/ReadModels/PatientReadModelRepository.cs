using EMR.Application.Common.Interfaces;
using EMR.Domain.ReadModels;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories.ReadModels;

/// <summary>
/// Specialized repository for patient summary read models
/// </summary>
public class PatientReadModelRepository : ReadModelRepository<PatientSummaryReadModel>, IPatientReadModelRepository
{
    public PatientReadModelRepository(ReadDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<PatientSummaryReadModel>> SearchAsync(
        string searchText,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Array.Empty<PatientSummaryReadModel>();
        }

        var search = searchText.Trim().ToLowerInvariant();

        return await _dbSet
            .AsNoTracking()
            .Where(p =>
                p.MRN.ToLower().Contains(search) ||
                p.FirstName.ToLower().Contains(search) ||
                p.LastName.ToLower().Contains(search) ||
                p.FullName.ToLower().Contains(search) ||
                (p.Email != null && p.Email.ToLower().Contains(search)) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(search)) ||
                p.SearchText.ToLower().Contains(search))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientSummaryReadModel>> GetByProviderAsync(
        Guid providerId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(p => p.PrimaryCareProviderId == providerId);

        if (activeOnly)
        {
            query = query.Where(p => p.Status == "Active");
        }

        return await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientSummaryReadModel>> GetWithActiveAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.ActiveAlertsCount > 0 && p.Status == "Active")
            .OrderByDescending(p => p.ActiveAlertsCount)
            .ThenBy(p => p.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientSummaryReadModel>> GetByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.Status == status)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<PatientSummaryReadModel?> GetByMRNAsync(
        string mrn,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == mrn, cancellationToken);
    }
}

/// <summary>
/// Specialized repository for patient detail read models
/// </summary>
public class PatientDetailReadModelRepository : ReadModelRepository<PatientDetailReadModel>, IPatientDetailReadModelRepository
{
    public PatientDetailReadModelRepository(ReadDbContext context) : base(context)
    {
    }

    public async Task<PatientDetailReadModel?> GetByMRNAsync(
        string mrn,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.MRN == mrn, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDetailReadModel>> GetWithActiveMedicationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.ActiveMedications.Any() && p.Status == "Active")
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDetailReadModel>> GetWithAllergyAsync(
        string allergen,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(p => p.ActiveAllergies.Any(a => a.Allergen.ToLower().Contains(allergen.ToLower())))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync(cancellationToken);
    }
}
