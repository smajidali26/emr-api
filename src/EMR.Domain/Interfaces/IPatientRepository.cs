using EMR.Domain.Entities;

namespace EMR.Domain.Interfaces;

/// <summary>
/// Repository interface for Patient entity operations
/// </summary>
public interface IPatientRepository : IRepository<Patient>
{
    /// <summary>
    /// Get patient by Medical Record Number (MRN)
    /// </summary>
    /// <param name="mrn">Medical Record Number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Patient entity or null if not found</returns>
    Task<Patient?> GetByMrnAsync(string mrn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search patients by criteria
    /// </summary>
    /// <param name="searchTerm">Search term (searches in name, MRN, email)</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of patients matching criteria</returns>
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Search patients by criteria with authorization filter
    /// SECURITY: Filters results at database level to only include authorized patients
    /// </summary>
    /// <param name="searchTerm">Search term (searches in name, MRN, email)</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="authorizedPatientIds">Set of patient IDs the user is authorized to view (null = no filter for admins)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of authorized patients matching criteria</returns>
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> SearchPatientsAsync(
        string? searchTerm,
        int pageNumber,
        int pageSize,
        IReadOnlySet<Guid>? authorizedPatientIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if MRN already exists in the system
    /// </summary>
    /// <param name="mrn">Medical Record Number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if MRN exists, false otherwise</returns>
    Task<bool> MrnExistsAsync(string mrn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get patients by email
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of patients with the given email</returns>
    Task<IReadOnlyList<Patient>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get patients by date of birth
    /// </summary>
    /// <param name="dateOfBirth">Date of birth</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of patients with the given date of birth</returns>
    Task<IReadOnlyList<Patient>> GetByDateOfBirthAsync(DateTime dateOfBirth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Find potential duplicate patients based on name and date of birth
    /// </summary>
    /// <param name="firstName">First name</param>
    /// <param name="lastName">Last name</param>
    /// <param name="dateOfBirth">Date of birth</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of potential duplicate patients</returns>
    Task<IReadOnlyList<Patient>> FindPotentialDuplicatesAsync(
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        CancellationToken cancellationToken = default);
}
