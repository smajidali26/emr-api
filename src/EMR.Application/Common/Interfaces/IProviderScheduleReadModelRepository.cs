using EMR.Domain.ReadModels;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Specialized repository for provider schedule read models
/// </summary>
public interface IProviderScheduleReadModelRepository : IReadModelRepository<ProviderScheduleReadModel>
{
    /// <summary>
    /// Gets schedule for a provider on a specific date
    /// </summary>
    Task<ProviderScheduleReadModel?> GetByProviderAndDateAsync(
        Guid providerId,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules for a provider for a date range
    /// </summary>
    Task<IReadOnlyList<ProviderScheduleReadModel>> GetByProviderRangeAsync(
        Guid providerId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all provider schedules for a specific date
    /// </summary>
    Task<IReadOnlyList<ProviderScheduleReadModel>> GetByDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets schedules by department for a date
    /// </summary>
    Task<IReadOnlyList<ProviderScheduleReadModel>> GetByDepartmentAndDateAsync(
        string department,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available schedules (with open slots) for a date and specialty
    /// </summary>
    Task<IReadOnlyList<ProviderScheduleReadModel>> GetAvailableBySpecialtyAsync(
        string specialty,
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets on-call providers for a date
    /// </summary>
    Task<IReadOnlyList<ProviderScheduleReadModel>> GetOnCallProvidersAsync(
        DateTime date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets providers with virtual availability
    /// </summary>
    Task<IReadOnlyList<ProviderScheduleReadModel>> GetVirtualAvailabilityAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets providers accepting new patients
    /// </summary>
    Task<IReadOnlyList<ProviderScheduleReadModel>> GetAcceptingNewPatientsAsync(
        DateTime date,
        string? specialty = null,
        CancellationToken cancellationToken = default);
}
