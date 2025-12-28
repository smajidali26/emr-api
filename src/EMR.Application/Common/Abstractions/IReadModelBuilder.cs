using EMR.Domain.ReadModels;

namespace EMR.Application.Common.Abstractions;

/// <summary>
/// Interface for rebuilding read models from event store
/// </summary>
/// <typeparam name="TReadModel">The read model type</typeparam>
public interface IReadModelBuilder<TReadModel> where TReadModel : BaseReadModel
{
    /// <summary>
    /// Rebuilds a single read model from its event history
    /// </summary>
    /// <param name="aggregateId">The aggregate ID to rebuild</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RebuildAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds all read models of this type
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RebuildAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuilds read models for a specific date range
    /// </summary>
    /// <param name="fromDate">Start date</param>
    /// <param name="toDate">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RebuildRangeAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}
