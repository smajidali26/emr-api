namespace EMR.Domain.Exceptions;

/// <summary>
/// Exception thrown when an entity is not found
/// </summary>
public class EntityNotFoundException : DomainException
{
    public string EntityName { get; }
    public object EntityId { get; }

    public EntityNotFoundException(string entityName, object entityId)
        : base($"{entityName} with ID '{entityId}' was not found.")
    {
        EntityName = entityName;
        EntityId = entityId;
    }

    public EntityNotFoundException(string entityName, object entityId, Exception innerException)
        : base($"{entityName} with ID '{entityId}' was not found.", innerException)
    {
        EntityName = entityName;
        EntityId = entityId;
    }
}
