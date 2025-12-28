using EMR.Domain.Common;

namespace EMR.Domain.Events.Order;

/// <summary>
/// Event raised when a new order is created in the system.
/// Orders can be medication orders, lab orders, imaging orders, etc.
/// </summary>
public sealed record OrderCreatedEvent : DomainEventBase
{
    /// <summary>
    /// The unique identifier of the order
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// The patient for whom the order is created
    /// </summary>
    public Guid PatientId { get; init; }

    /// <summary>
    /// The encounter during which the order was created
    /// </summary>
    public Guid EncounterId { get; init; }

    /// <summary>
    /// The provider who created the order
    /// </summary>
    public Guid ProviderId { get; init; }

    /// <summary>
    /// Type of order (Medication, Lab, Imaging, Procedure, etc.)
    /// </summary>
    public string OrderType { get; init; } = string.Empty;

    /// <summary>
    /// Order category for classification
    /// </summary>
    public string? OrderCategory { get; init; }

    /// <summary>
    /// Description of what is ordered
    /// </summary>
    public string OrderDescription { get; init; } = string.Empty;

    /// <summary>
    /// Priority level (Routine, Urgent, STAT, etc.)
    /// </summary>
    public string Priority { get; init; } = "Routine";

    /// <summary>
    /// When the order was placed
    /// </summary>
    public DateTime OrderedAt { get; init; }

    /// <summary>
    /// When the order should be executed
    /// </summary>
    public DateTime? ScheduledFor { get; init; }

    /// <summary>
    /// Clinical indication or reason for the order
    /// </summary>
    public string? ClinicalIndication { get; init; }

    /// <summary>
    /// Additional instructions for order execution
    /// </summary>
    public string? Instructions { get; init; }

    /// <summary>
    /// Order details as JSON (specific to order type)
    /// </summary>
    public string? OrderDetailsJson { get; init; }

    public OrderCreatedEvent()
    {
    }

    public OrderCreatedEvent(
        Guid orderId,
        Guid patientId,
        Guid encounterId,
        Guid providerId,
        string orderType,
        string orderDescription,
        DateTime orderedAt,
        string priority = "Routine",
        string? userId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(userId, correlationId, causationId)
    {
        OrderId = orderId;
        PatientId = patientId;
        EncounterId = encounterId;
        ProviderId = providerId;
        OrderType = orderType;
        OrderDescription = orderDescription;
        OrderedAt = orderedAt;
        Priority = priority;
    }
}
