using EMR.Infrastructure.EventSourcing;
using EMR.Infrastructure.EventSourcing.Configurations;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EMR.Infrastructure.Data;

/// <summary>
/// Database context specifically for Event Sourcing.
/// Separates event store concerns from main application data.
/// </summary>
public class EventStoreDbContext : DbContext
{
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Event Store entries for event sourcing
    /// </summary>
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();

    /// <summary>
    /// Snapshot entries for aggregate snapshots
    /// </summary>
    public DbSet<SnapshotEntry> Snapshots => Set<SnapshotEntry>();

    /// <summary>
    /// Outbox messages for transactional outbox pattern
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply event sourcing-specific configurations
        modelBuilder.ApplyConfiguration(new EventStoreEntryConfiguration());
        modelBuilder.ApplyConfiguration(new SnapshotEntryConfiguration());
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
