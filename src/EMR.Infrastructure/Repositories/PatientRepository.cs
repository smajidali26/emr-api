using EMR.Application.Common.Interfaces;
using EMR.Domain.Entities;
using EMR.Domain.Interfaces;
using EMR.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Patient entity
/// </summary>
public class PatientRepository : Repository<Patient>, IPatientRepository
{
    public PatientRepository(ApplicationDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService)
    {
    }

    /// <summary>
    /// Get patient by Medical Record Number (MRN)
    /// </summary>
    public async Task<Patient?> GetByMrnAsync(string mrn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrn))
            return null;

        return await _dbSet
            .FirstOrDefaultAsync(p => p.MedicalRecordNumber.Value == mrn.ToUpperInvariant(), cancellationToken);
    }

    /// <summary>
    /// Search patients by criteria
    /// SECURITY FIX: Task #2 - Add input validation for search params (Maria Rodriguez - 8h)
    /// Defense-in-depth: Validate parameters at repository level as well
    /// </summary>
    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        // SECURITY: Validate parameters to prevent SQL injection and resource exhaustion
        // This is defense-in-depth - parameters should already be validated at the API layer
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than or equal to 1", nameof(pageNumber));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

        var query = _dbSet.AsQueryable();

        // Apply search filter if search term is provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // SECURITY: Additional validation - search term length check
            if (searchTerm.Length > 100)
                throw new ArgumentException("Search term must be 100 characters or less", nameof(searchTerm));

            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(lowerSearchTerm) ||
                p.LastName.ToLower().Contains(lowerSearchTerm) ||
                p.MedicalRecordNumber.Value.ToLower().Contains(lowerSearchTerm) ||
                (p.Email != null && p.Email.ToLower().Contains(lowerSearchTerm)));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (patients, totalCount);
    }

    /// <summary>
    /// Search patients by criteria with authorization filter
    /// SECURITY: Filters results at database level to only include authorized patients
    /// SECURITY FIX: Task #2 - Add input validation for search params (Maria Rodriguez - 8h)
    /// </summary>
    public async Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        IReadOnlySet<Guid>? authorizedPatientIds,
        CancellationToken cancellationToken = default)
    {
        // SECURITY: Validate parameters to prevent SQL injection and resource exhaustion
        // This is defense-in-depth - parameters should already be validated at the API layer
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be greater than or equal to 1", nameof(pageNumber));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

        var query = _dbSet.AsQueryable();

        // SECURITY: Apply authorization filter at database level
        // null = no filter (admin access), otherwise filter to authorized IDs only
        if (authorizedPatientIds != null)
        {
            if (authorizedPatientIds.Count == 0)
            {
                // User has no authorized patients - return empty result
                return (new List<Patient>(), 0);
            }

            query = query.Where(p => authorizedPatientIds.Contains(p.Id));
        }

        // Apply search filter if search term is provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // SECURITY: Additional validation - search term length check
            if (searchTerm.Length > 100)
                throw new ArgumentException("Search term must be 100 characters or less", nameof(searchTerm));

            var lowerSearchTerm = searchTerm.ToLower();
            query = query.Where(p =>
                p.FirstName.ToLower().Contains(lowerSearchTerm) ||
                p.LastName.ToLower().Contains(lowerSearchTerm) ||
                p.MedicalRecordNumber.Value.ToLower().Contains(lowerSearchTerm) ||
                (p.Email != null && p.Email.ToLower().Contains(lowerSearchTerm)));
        }

        // Get total count (of authorized patients matching search)
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (patients, totalCount);
    }

    /// <summary>
    /// Check if MRN already exists in the system
    /// </summary>
    public async Task<bool> MrnExistsAsync(string mrn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mrn))
            return false;

        return await _dbSet.AnyAsync(p => p.MedicalRecordNumber.Value == mrn.ToUpperInvariant(), cancellationToken);
    }

    /// <summary>
    /// Get patients by email
    /// </summary>
    public async Task<IReadOnlyList<Patient>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new List<Patient>();

        return await _dbSet
            .Where(p => p.Email != null && p.Email == email.ToLowerInvariant())
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get patients by date of birth
    /// </summary>
    public async Task<IReadOnlyList<Patient>> GetByDateOfBirthAsync(DateTime dateOfBirth, CancellationToken cancellationToken = default)
    {
        var searchDate = dateOfBirth.Date;
        return await _dbSet
            .Where(p => p.DateOfBirth == searchDate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Find potential duplicate patients based on name and date of birth
    /// </summary>
    public async Task<IReadOnlyList<Patient>> FindPotentialDuplicatesAsync(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            return new List<Patient>();

        var searchDate = dateOfBirth.Date;
        var lowerFirstName = firstName.ToLower();
        var lowerLastName = lastName.ToLower();

        return await _dbSet
            .Where(p =>
                p.FirstName.ToLower() == lowerFirstName &&
                p.LastName.ToLower() == lowerLastName &&
                p.DateOfBirth == searchDate)
            .ToListAsync(cancellationToken);
    }
}
