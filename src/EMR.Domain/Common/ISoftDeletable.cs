namespace EMR.Domain.Common;

/// <summary>
/// Interface for entities that support soft deletion
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }
    void MarkAsDeleted(string userId);
    void Restore();
}
