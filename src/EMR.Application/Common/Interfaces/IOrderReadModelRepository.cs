using EMR.Domain.ReadModels;

namespace EMR.Application.Common.Interfaces;

/// <summary>
/// Specialized repository for order read models (worklist)
/// </summary>
public interface IOrderReadModelRepository : IReadModelRepository<ActiveOrdersReadModel>
{
    /// <summary>
    /// Gets active orders for a patient
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetActiveByPatientAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders for an encounter
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetByEncounterAsync(
        Guid encounterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders by type and status
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetByTypeAndStatusAsync(
        string orderType,
        string status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders assigned to a specific user/tech
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetAssignedToAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders for a performing department
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetByDepartmentAsync(
        string department,
        string? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets STAT/urgent orders
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetUrgentOrdersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets overdue orders
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetOverdueOrdersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders requiring authorization
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetRequiringAuthorizationAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets critical orders with results
    /// </summary>
    Task<IReadOnlyList<ActiveOrdersReadModel>> GetCriticalResultsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets order by order number
    /// </summary>
    Task<ActiveOrdersReadModel?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken = default);
}
