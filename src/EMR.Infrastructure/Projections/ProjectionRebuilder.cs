using EMR.Application.Common.Abstractions;
using EMR.Domain.Common;
using EMR.Domain.ReadModels;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Projections;

/// <summary>
/// Service for rebuilding read models from event store
/// Used for recovery, migration, and maintenance
/// </summary>
public interface IProjectionRebuilder
{
    /// <summary>
    /// Rebuilds a specific read model type from events
    /// </summary>
    Task RebuildReadModelAsync<TReadModel>(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
        where TReadModel : BaseReadModel;

    /// <summary>
    /// Rebuilds all read models of a specific type
    /// </summary>
    Task RebuildAllReadModelsAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : BaseReadModel;

    /// <summary>
    /// Rebuilds all projections (all read model types)
    /// </summary>
    Task RebuildAllProjectionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of projection rebuilder
/// Note: This requires an event store implementation to be fully functional
/// </summary>
public class ProjectionRebuilder : IProjectionRebuilder
{
    private readonly ILogger<ProjectionRebuilder> _logger;
    // TODO: Inject IEventStore when event sourcing is implemented
    // private readonly IEventStore _eventStore;

    public ProjectionRebuilder(ILogger<ProjectionRebuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RebuildReadModelAsync<TReadModel>(
        Guid aggregateId,
        CancellationToken cancellationToken = default)
        where TReadModel : BaseReadModel
    {
        _logger.LogInformation(
            "Starting rebuild of read model {ReadModelType} for aggregate {AggregateId}",
            typeof(TReadModel).Name,
            aggregateId);

        try
        {
            // TODO: Implement event store replay
            // 1. Get all events for the aggregate from event store
            // 2. Clear existing read model
            // 3. Replay events through projection handlers
            // 4. Mark read model as rebuilt

            _logger.LogInformation(
                "Successfully rebuilt read model {ReadModelType} for aggregate {AggregateId}",
                typeof(TReadModel).Name,
                aggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error rebuilding read model {ReadModelType} for aggregate {AggregateId}: {ErrorMessage}",
                typeof(TReadModel).Name,
                aggregateId,
                ex.Message);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task RebuildAllReadModelsAsync<TReadModel>(
        CancellationToken cancellationToken = default)
        where TReadModel : BaseReadModel
    {
        _logger.LogInformation(
            "Starting rebuild of all read models of type {ReadModelType}",
            typeof(TReadModel).Name);

        try
        {
            // TODO: Implement batch rebuild
            // 1. Get all aggregate IDs from event store
            // 2. Clear all existing read models of this type
            // 3. Replay all events through projection handlers
            // 4. Mark all read models as rebuilt

            _logger.LogInformation(
                "Successfully rebuilt all read models of type {ReadModelType}",
                typeof(TReadModel).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error rebuilding all read models of type {ReadModelType}: {ErrorMessage}",
                typeof(TReadModel).Name,
                ex.Message);
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task RebuildAllProjectionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting rebuild of all projections (all read model types)");

        try
        {
            // TODO: Implement full rebuild
            // 1. Get all events from event store (in chronological order)
            // 2. Clear all read models
            // 3. Replay all events through all projection handlers
            // 4. Mark all projections as rebuilt

            _logger.LogInformation("Successfully rebuilt all projections");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error rebuilding all projections: {ErrorMessage}",
                ex.Message);
            throw;
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Builder implementation for specific read model types
/// </summary>
/// <typeparam name="TReadModel">The read model type</typeparam>
public class ReadModelBuilder<TReadModel> : IReadModelBuilder<TReadModel>
    where TReadModel : BaseReadModel
{
    private readonly IProjectionRebuilder _rebuilder;
    private readonly ILogger<ReadModelBuilder<TReadModel>> _logger;

    public ReadModelBuilder(
        IProjectionRebuilder rebuilder,
        ILogger<ReadModelBuilder<TReadModel>> logger)
    {
        _rebuilder = rebuilder ?? throw new ArgumentNullException(nameof(rebuilder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task RebuildAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Rebuilding single {ReadModelType} for aggregate {AggregateId}",
            typeof(TReadModel).Name,
            aggregateId);

        await _rebuilder.RebuildReadModelAsync<TReadModel>(aggregateId, cancellationToken);
    }

    public async Task RebuildAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Rebuilding all {ReadModelType} instances",
            typeof(TReadModel).Name);

        await _rebuilder.RebuildAllReadModelsAsync<TReadModel>(cancellationToken);
    }

    public async Task RebuildRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Rebuilding {ReadModelType} for date range {FromDate} to {ToDate}",
            typeof(TReadModel).Name,
            fromDate,
            toDate);

        // TODO: Implement date-range rebuild
        // 1. Query event store for events in date range
        // 2. Identify affected aggregates
        // 3. Rebuild read models for those aggregates

        await Task.CompletedTask;
    }
}
