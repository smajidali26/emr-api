using EMR.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EMR.Infrastructure.Projections;

/// <summary>
/// Manages eventual consistency between write and read models
/// Tracks projection state and handles retries
/// </summary>
public interface IEventualConsistencyManager
{
    /// <summary>
    /// Registers that a projection is being processed
    /// </summary>
    Task RegisterProjectionStartAsync(Guid eventId, string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a projection as completed successfully
    /// </summary>
    Task MarkProjectionCompleteAsync(Guid eventId, string projectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a projection as failed
    /// </summary>
    Task MarkProjectionFailedAsync(Guid eventId, string projectionName, string errorMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed projections that need retry
    /// </summary>
    Task<IReadOnlyList<FailedProjection>> GetFailedProjectionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all projections for an event are complete
    /// </summary>
    Task<bool> AreAllProjectionsCompleteAsync(Guid eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of eventual consistency manager
/// In production, this should be backed by a persistent store (database, Redis, etc.)
/// </summary>
public class EventualConsistencyManager : IEventualConsistencyManager
{
    private readonly ILogger<EventualConsistencyManager> _logger;
    private readonly ConcurrentDictionary<string, ProjectionState> _projectionStates;
    private readonly ConcurrentDictionary<Guid, HashSet<string>> _eventProjections;

    public EventualConsistencyManager(ILogger<EventualConsistencyManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectionStates = new ConcurrentDictionary<string, ProjectionState>();
        _eventProjections = new ConcurrentDictionary<Guid, HashSet<string>>();
    }

    public Task RegisterProjectionStartAsync(
        Guid eventId,
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(eventId, projectionName);
        var state = new ProjectionState
        {
            EventId = eventId,
            ProjectionName = projectionName,
            Status = ProjectionStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        _projectionStates[key] = state;

        // Track all projections for this event
        _eventProjections.AddOrUpdate(
            eventId,
            new HashSet<string> { projectionName },
            (_, existing) =>
            {
                existing.Add(projectionName);
                return existing;
            });

        _logger.LogDebug(
            "Registered projection start: Event {EventId}, Projection {ProjectionName}",
            eventId,
            projectionName);

        return Task.CompletedTask;
    }

    public Task MarkProjectionCompleteAsync(
        Guid eventId,
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(eventId, projectionName);

        if (_projectionStates.TryGetValue(key, out var state))
        {
            state.Status = ProjectionStatus.Completed;
            state.CompletedAt = DateTime.UtcNow;

            _logger.LogDebug(
                "Marked projection complete: Event {EventId}, Projection {ProjectionName}",
                eventId,
                projectionName);
        }

        return Task.CompletedTask;
    }

    public Task MarkProjectionFailedAsync(
        Guid eventId,
        string projectionName,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var key = GetKey(eventId, projectionName);

        if (_projectionStates.TryGetValue(key, out var state))
        {
            state.Status = ProjectionStatus.Failed;
            state.FailedAt = DateTime.UtcNow;
            state.ErrorMessage = errorMessage;
            state.RetryCount++;

            _logger.LogWarning(
                "Marked projection failed: Event {EventId}, Projection {ProjectionName}, Error: {ErrorMessage}, RetryCount: {RetryCount}",
                eventId,
                projectionName,
                errorMessage,
                state.RetryCount);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<FailedProjection>> GetFailedProjectionsAsync(
        CancellationToken cancellationToken = default)
    {
        var failedProjections = _projectionStates.Values
            .Where(s => s.Status == ProjectionStatus.Failed && s.RetryCount < 5) // Max 5 retries
            .Select(s => new FailedProjection
            {
                EventId = s.EventId,
                ProjectionName = s.ProjectionName,
                ErrorMessage = s.ErrorMessage ?? string.Empty,
                RetryCount = s.RetryCount,
                FailedAt = s.FailedAt ?? DateTime.UtcNow
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<FailedProjection>>(failedProjections);
    }

    public Task<bool> AreAllProjectionsCompleteAsync(
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        if (!_eventProjections.TryGetValue(eventId, out var projectionNames))
        {
            return Task.FromResult(true); // No projections registered
        }

        var allComplete = projectionNames.All(projectionName =>
        {
            var key = GetKey(eventId, projectionName);
            return _projectionStates.TryGetValue(key, out var state) &&
                   state.Status == ProjectionStatus.Completed;
        });

        return Task.FromResult(allComplete);
    }

    private static string GetKey(Guid eventId, string projectionName)
    {
        return $"{eventId}:{projectionName}";
    }
}

#region Supporting Types

public class ProjectionState
{
    public Guid EventId { get; set; }
    public string ProjectionName { get; set; } = string.Empty;
    public ProjectionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
}

public enum ProjectionStatus
{
    InProgress,
    Completed,
    Failed
}

public class FailedProjection
{
    public Guid EventId { get; set; }
    public string ProjectionName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime FailedAt { get; set; }
}

#endregion
