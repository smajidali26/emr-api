using EMR.Infrastructure.Projections;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Data;

/// <summary>
/// Extension to ApplicationDbContext that dispatches domain events
/// This ensures read models are updated after write model changes
/// </summary>
public static class ApplicationDbContextExtensions
{
    /// <summary>
    /// Saves changes and dispatches domain events to update read models
    /// </summary>
    public static async Task<int> SaveChangesWithEventsAsync(
        this ApplicationDbContext context,
        IDomainEventDispatcher eventDispatcher,
        CancellationToken cancellationToken = default)
    {
        // Save changes to write model
        var result = await context.SaveChangesAsync(cancellationToken);

        // Dispatch domain events to update read models
        await eventDispatcher.DispatchEventsAsync(cancellationToken);

        return result;
    }
}
