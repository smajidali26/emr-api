using EMR.Domain.ReadModels;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Specialized repository for encounter read models
/// </summary>
public interface IEncounterReadModelRepository : IReadModelRepository<EncounterListReadModel>
{
    /// <summary>
    /// Gets encounters for a specific patient
    /// </summary>
    Task<IReadOnlyList<EncounterListReadModel>> GetByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets encounters for a specific provider
    /// </summary>
    Task<IReadOnlyList<EncounterListReadModel>> GetByProviderAsync(
        Guid providerId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets encounters by status
    /// </summary>
    Task<IReadOnlyList<EncounterListReadModel>> GetByStatusAsync(
        string status,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets encounters by department
    /// </summary>
    Task<IReadOnlyList<EncounterListReadModel>> GetByDepartmentAsync(
        string department,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets encounter by encounter number
    /// </summary>
    Task<EncounterListReadModel?> GetByEncounterNumberAsync(
        string encounterNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active/in-progress encounters
    /// </summary>
    Task<IReadOnlyList<EncounterListReadModel>> GetActiveEncountersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets encounters scheduled for a specific date
    /// </summary>
    Task<IReadOnlyList<EncounterListReadModel>> GetScheduledForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default);
}
