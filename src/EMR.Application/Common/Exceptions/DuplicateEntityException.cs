namespace EMR.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a duplicate entity
/// </summary>
public class DuplicateEntityException : Exception
{
    public string PropertyName { get; }

    public DuplicateEntityException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }

    public DuplicateEntityException(string message, string propertyName, Exception innerException)
        : base(message, innerException)
    {
        PropertyName = propertyName;
    }
}
