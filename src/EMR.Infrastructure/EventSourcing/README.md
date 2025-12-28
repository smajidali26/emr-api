# Event Sourcing Implementation

This directory contains the complete Event Sourcing infrastructure for the EMR system.

## Overview

Event Sourcing is an architectural pattern where state changes are stored as a sequence of immutable events. Instead of storing the current state, we store all the events that led to that state.

## Components

### Domain Layer (`EMR.Domain`)

#### Core Interfaces and Base Classes
- **IDomainEvent**: Marker interface for all domain events
- **DomainEventBase**: Base class providing event metadata (ID, timestamp, user, correlation/causation IDs)
- **AggregateRoot**: Base class for event-sourced aggregates with event collection

#### Domain Events
Located in `EMR.Domain/Events/`:
- **Patient Events**
  - `PatientRegisteredEvent`: When a patient is first registered
  - `PatientDemographicsUpdatedEvent`: When patient information changes

- **Encounter Events**
  - `EncounterStartedEvent`: When a patient visit begins
  - `EncounterCompletedEvent`: When a patient visit ends

- **Order Events**
  - `OrderCreatedEvent`: When an order is placed
  - `OrderStatusChangedEvent`: When an order status changes

### Application Layer (`EMR.Application`)

#### Interfaces
- **IEventStore**: Core event store operations (append, retrieve events)
- **IEventPublisher**: Publishing events to handlers and message bus
- **ISnapshotStore**: Snapshot management for performance optimization
- **IEventReplay**: Event replay for debugging and projections
- **ConcurrencyException**: Exception for optimistic concurrency conflicts

#### Event Handlers
Located in `EMR.Application/EventHandlers/`:
- Event handlers implement `INotificationHandler<TEvent>` from MediatR
- Example: `PatientRegisteredEventHandler` processes patient registration events

### Infrastructure Layer (`EMR.Infrastructure`)

#### Persistence
- **EventStoreEntry**: Database entity for storing events
- **SnapshotEntry**: Database entity for storing snapshots
- **OutboxMessage**: Database entity for outbox pattern
- **EventStoreDbContext**: Dedicated DbContext for event sourcing

#### Implementations
- **SqlEventStore**: SQL-based event store with optimistic concurrency
- **SqlSnapshotStore**: Snapshot storage and retrieval
- **EventSerializer**: JSON serialization/deserialization with type resolution
- **MediatREventPublisher**: MediatR-based event publishing
- **EventReplay**: Event replay for aggregate reconstruction
- **OutboxProcessor**: Background service for reliable event publishing
- **DomainEventDispatcherInterceptor**: EF interceptor for automatic event publishing
- **EventSourcedRepository<T>**: Generic repository for event-sourced aggregates

## Usage

### 1. Register Event Sourcing Services

In your `Program.cs` or startup configuration:

```csharp
services.AddEventSourcing(configuration);
```

### 2. Create an Event-Sourced Aggregate

```csharp
public class Patient : AggregateRoot
{
    public string MedicalRecordNumber { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    public static Patient Register(
        string mrn,
        string firstName,
        string lastName,
        string userId)
    {
        var patient = new Patient();
        var @event = new PatientRegisteredEvent(
            Guid.NewGuid(),
            mrn,
            firstName,
            lastName,
            DateTime.UtcNow,
            "M",
            userId);

        patient.RaiseEvent(@event);
        return patient;
    }

    protected override void ApplyEvent(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case PatientRegisteredEvent e:
                Id = e.PatientId;
                MedicalRecordNumber = e.MedicalRecordNumber;
                FirstName = e.FirstName;
                LastName = e.LastName;
                break;
        }
        base.ApplyEvent(domainEvent);
    }
}
```

### 3. Use Event-Sourced Repository

```csharp
// Save aggregate
var repository = new EventSourcedRepository<Patient>(eventStore, snapshotStore, logger);
var patient = Patient.Register("MRN001", "John", "Doe", "user123");
await repository.SaveAsync(patient);

// Load aggregate
var loadedPatient = await repository.GetByIdAsync(patientId);
```

### 4. Handle Events with MediatR

```csharp
public class PatientRegisteredEventHandler : INotificationHandler<PatientRegisteredEvent>
{
    public async Task Handle(PatientRegisteredEvent notification, CancellationToken cancellationToken)
    {
        // Update read models
        // Send notifications
        // Trigger other processes
    }
}
```

### 5. Replay Events

```csharp
// Replay aggregate to specific point in time
var historicalState = await eventReplay.ReplayAggregateAsOfAsync<Patient>(
    patientId,
    DateTime.Parse("2024-01-01"));

// Rebuild projections
await eventReplay.ReplayAllEventsAsync(async (domainEvent) =>
{
    // Process event to rebuild read model
});
```

## Key Features

### 1. Optimistic Concurrency Control
Events include version numbers. Concurrent updates are detected and rejected with `ConcurrencyException`.

### 2. Event Versioning
Each event has an `EventVersion` property for schema evolution:
- Version 1: Original schema
- Version 2+: Updated schema with migration logic

### 3. Correlation and Causation Tracking
- **CorrelationId**: Groups related events across aggregates
- **CausationId**: Links cause-and-effect event chains

### 4. Snapshots
Snapshots are automatically taken every 50 events to optimize replay performance.

### 5. Outbox Pattern
Events are stored in an outbox table and processed asynchronously for reliable external publishing.

### 6. Temporal Queries
Query aggregate state at any point in time using event replay.

### 7. Complete Audit Trail
All events are immutable and provide a complete audit history.

## Database Schema

### EventStore Table
- Stores all domain events
- Indexed by aggregate ID, version, correlation ID, and timestamp
- Uses JSONB for event data (PostgreSQL)

### Snapshots Table
- Stores aggregate snapshots
- Indexed by aggregate ID and version

### OutboxMessages Table
- Stores events pending external publication
- Supports retry with exponential backoff

## Performance Considerations

1. **Snapshots**: Reduce event replay overhead
2. **Indexes**: Optimized for common query patterns
3. **JSONB**: Efficient JSON storage and querying (PostgreSQL)
4. **Batch Processing**: Outbox processor handles events in batches
5. **Async Processing**: Event publishing is asynchronous

## Best Practices

1. **Event Immutability**: Never modify published events
2. **Event Versioning**: Plan for schema evolution from the start
3. **Idempotent Handlers**: Event handlers should be idempotent
4. **Fine-grained Events**: Prefer specific events over generic ones
5. **Correlation IDs**: Always set correlation IDs for traceability
6. **Snapshot Strategy**: Monitor aggregate size and adjust snapshot frequency

## Integration with Audit Logging (Feature 54)

Event Sourcing provides the foundation for comprehensive audit logging:
- Every state change is recorded as an event
- Events include user, timestamp, and reason
- Complete audit trail is automatically maintained
- Temporal queries enable "time travel" debugging

## Troubleshooting

### Concurrency Conflicts
If you encounter `ConcurrencyException`, the aggregate was modified by another process. Reload and retry.

### Event Replay Performance
If replay is slow, check:
- Snapshot frequency (default: every 50 events)
- Database indexes
- Event payload size

### Outbox Processing Delays
Check:
- OutboxProcessor is running
- No errors in outbox messages table
- Message bus connectivity

## Future Enhancements

1. Event upcasting for version migration
2. Event archiving for old events
3. CQRS read model projections
4. Event store sharding for scalability
5. Event-driven microservices integration
