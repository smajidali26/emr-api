namespace EMR.Application.Abstractions.EventSourcing;

/// <summary>
/// Exception thrown when a concurrency conflict is detected in the event store.
/// Occurs when the expected version doesn't match the actual version.
/// </summary>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// The aggregate ID where the conflict occurred
    /// </summary>
    public Guid AggregateId { get; }

    /// <summary>
    /// The expected version number
    /// </summary>
    public int ExpectedVersion { get; }

    /// <summary>
    /// The actual version number in the store
    /// </summary>
    public int ActualVersion { get; }

    public ConcurrencyException(Guid aggregateId, int expectedVersion, int actualVersion)
        : base($"Concurrency conflict for aggregate {aggregateId}. Expected version {expectedVersion}, but actual version is {actualVersion}.")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public ConcurrencyException(Guid aggregateId, int expectedVersion, int actualVersion, string message)
        : base(message)
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }

    public ConcurrencyException(Guid aggregateId, int expectedVersion, int actualVersion, string message, Exception innerException)
        : base(message, innerException)
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
