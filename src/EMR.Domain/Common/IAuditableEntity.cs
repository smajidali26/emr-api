namespace EMR.Domain.Common;

/// <summary>
/// Interface for entities that support audit tracking
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }
    string CreatedBy { get; }
    DateTime? UpdatedAt { get; }
    string? UpdatedBy { get; }
}
