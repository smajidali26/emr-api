using EMR.Domain.Common;

namespace EMR.Domain.Events.Order;

/// <summary>
/// Event raised when an order's status changes.
/// Tracks the lifecycle of an order from creation to completion.
/// </summary>
public sealed record OrderStatusChangedEvent : DomainEventBase
{
    /// <summary>
    /// The unique identifier of the order
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// The patient for whom the order exists
    /// </summary>
    public Guid PatientId { get; init; }

    /// <summary>
    /// Previous status of the order
    /// </summary>
    public string PreviousStatus { get; init; } = string.Empty;

    /// <summary>
    /// New status of the order
    /// </summary>
    public string NewStatus { get; init; } = string.Empty;

    /// <summary>
    /// Reason for the status change
    /// </summary>
    public string? StatusChangeReason { get; init; }

    /// <summary>
    /// When the status changed
    /// </summary>
    public DateTime StatusChangedAt { get; init; }

    /// <summary>
    /// User who changed the status
    /// </summary>
    public string? ChangedByUserId { get; init; }

    /// <summary>
    /// Additional notes about the status change
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Whether this status change requires approval
    /// </summary>
    public bool RequiresApproval { get; init; }

    /// <summary>
    /// Results or outcome data (for completed orders)
    /// </summary>
    public string? ResultData { get; init; }

    public OrderStatusChangedEvent()
    {
    }

    public OrderStatusChangedEvent(
        Guid orderId,
        Guid patientId,
        string previousStatus,
        string newStatus,
        DateTime statusChangedAt,
        string? userId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(userId, correlationId, causationId)
    {
        OrderId = orderId;
        PatientId = patientId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        StatusChangedAt = statusChangedAt;
        ChangedByUserId = userId;
    }
}
