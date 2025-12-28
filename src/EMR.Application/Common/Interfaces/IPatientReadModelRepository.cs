using EMR.Domain.ReadModels;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Specialized repository for patient read models
/// </summary>
public interface IPatientReadModelRepository : IReadModelRepository<PatientSummaryReadModel>
{
    /// <summary>
    /// Searches patients by text (name, MRN, email, phone, etc.)
    /// </summary>
    Task<IReadOnlyList<PatientSummaryReadModel>> SearchAsync(
        string searchText,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets patients by provider
    /// </summary>
    Task<IReadOnlyList<PatientSummaryReadModel>> GetByProviderAsync(
        Guid providerId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets patients with active alerts
    /// </summary>
    Task<IReadOnlyList<PatientSummaryReadModel>> GetWithActiveAlertsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets patients by status
    /// </summary>
    Task<IReadOnlyList<PatientSummaryReadModel>> GetByStatusAsync(
        string status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets patient by MRN
    /// </summary>
    Task<PatientSummaryReadModel?> GetByMRNAsync(
        string mrn,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Specialized repository for patient detail read models
/// </summary>
public interface IPatientDetailReadModelRepository : IReadModelRepository<PatientDetailReadModel>
{
    /// <summary>
    /// Gets patient detail by MRN
    /// </summary>
    Task<PatientDetailReadModel?> GetByMRNAsync(
        string mrn,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets patients with active medications
    /// </summary>
    Task<IReadOnlyList<PatientDetailReadModel>> GetWithActiveMedicationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets patients with specific allergy
    /// </summary>
    Task<IReadOnlyList<PatientDetailReadModel>> GetWithAllergyAsync(
        string allergen,
        CancellationToken cancellationToken = default);
}
