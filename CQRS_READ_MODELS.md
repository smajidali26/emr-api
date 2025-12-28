# CQRS Read Model Projections - Implementation Guide

## Overview

This document describes the implementation of CQRS (Command Query Responsibility Segregation) Read Model Projections for the EMR system. The implementation provides a complete infrastructure for separating read and write operations, enabling:

- **Optimized Query Performance**: Denormalized read models tailored for specific query patterns
- **Scalability**: Separate read and write databases
- **Eventual Consistency**: Event-driven updates with consistency tracking
- **Rebuild Capability**: Reconstruct read models from event history
- **Flexibility**: Easy to add new read models without affecting write side

## Architecture

```
Write Side (Commands):
Command -> Handler -> Aggregate -> Domain Event -> Event Store

Read Side (Queries):
Domain Event -> Projection Handler -> Read Model -> Query Handler -> DTO

Consistency:
Write DB -> Domain Events -> MediatR -> Projection Handlers -> Read DB
```

## Project Structure

### Domain Layer (EMR.Domain)

#### Common Infrastructure
- **`IDomainEvent`**: Interface for all domain events (already existed)
- **`DomainEvent`**: Base class for domain events (already existed)
- **`AggregateRoot`**: Base class for aggregates that raise events (already existed)

#### Read Models (`EMR.Domain/ReadModels/`)
- **`BaseReadModel`**: Base class for all read models with versioning
- **`PatientSummaryReadModel`**: Optimized for patient lists and search
- **`PatientDetailReadModel`**: Comprehensive patient details with nested data
- **`EncounterListReadModel`**: Encounter history and lists
- **`ActiveOrdersReadModel`**: Order worklist with denormalized data
- **`ProviderScheduleReadModel`**: Provider scheduling and availability

### Application Layer (EMR.Application)

#### Abstractions (`Common/Abstractions/`)
- **`IProjection<TEvent>`**: Interface for event projections
- **`IProjectionHandler<TEvent>`**: Handler for processing events
- **`IReadModelBuilder<TReadModel>`**: Interface for rebuilding read models

#### Repository Interfaces (`Common/Interfaces/`)
- **`IReadModelRepository<TReadModel>`**: Generic read model repository
- **`IPatientReadModelRepository`**: Patient-specific queries
- **`IPatientDetailReadModelRepository`**: Patient detail queries
- **`IEncounterReadModelRepository`**: Encounter-specific queries
- **`IOrderReadModelRepository`**: Order worklist queries
- **`IProviderScheduleReadModelRepository`**: Schedule queries

#### Query Handlers (`Features/Patients/Queries/`)
- **`GetPatientSummaryQuery`**: Get patient summary by ID
- **`SearchPatientsQuery`**: Full-text patient search
- **`GetPatientDetailQuery`**: Get detailed patient information

### Infrastructure Layer (EMR.Infrastructure)

#### Data Layer (`Data/`)
- **`ReadDbContext`**: Separate database context for read models
  - Configured with `read` schema
  - No tracking for optimal query performance
  - JSON columns for complex nested data
  - Comprehensive indexes for query optimization

#### Repositories (`Repositories/ReadModels/`)
- **`ReadModelRepository<TReadModel>`**: Generic implementation
- **`PatientReadModelRepository`**: Patient summary repository
- **`PatientDetailReadModelRepository`**: Patient detail repository
- **`EncounterReadModelRepository`**: Encounter repository
- **`OrderReadModelRepository`**: Order repository
- **`ProviderScheduleReadModelRepository`**: Schedule repository

#### Projections (`Projections/`)
- **`ProjectionHandlerBase<TEvent>`**: Base class for projection handlers
- **`EventualConsistencyManager`**: Tracks projection state and retries
- **`DomainEventDispatcher`**: Dispatches events after persistence
- **`ProjectionRebuilder`**: Rebuilds read models from event store
- **`ReadModelBuilder<TReadModel>`**: Type-specific rebuilder

#### Configuration (`Configuration/`)
- **`CqrsConfiguration`**: DI configuration for CQRS infrastructure

## Usage Guide

### 1. Configuring CQRS in Startup

```csharp
// In Program.cs or Startup.cs
using EMR.Infrastructure.Configuration;

// Add CQRS read model infrastructure
services.AddCqrsReadModels(configuration);

// Optional: Use separate read database connection
// services.AddSeparateReadDatabase(readConnectionString);

// Initialize read database on startup
await app.Services.InitializeReadDatabaseAsync();
```

### 2. Creating a Projection Handler

```csharp
using EMR.Infrastructure.Projections;
using EMR.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

public class PatientCreatedProjectionHandler
    : ProjectionHandlerBase<PatientCreatedEvent>
{
    private readonly IPatientReadModelRepository _repository;

    public PatientCreatedProjectionHandler(
        IPatientReadModelRepository repository,
        ILogger<PatientCreatedProjectionHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    protected override async Task ProjectAsync(
        PatientCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var readModel = new PatientSummaryReadModel
        {
            Id = domainEvent.PatientId,
            MRN = domainEvent.MRN,
            FirstName = domainEvent.FirstName,
            LastName = domainEvent.LastName,
            FullName = $"{domainEvent.FirstName} {domainEvent.LastName}",
            DateOfBirth = domainEvent.DateOfBirth,
            Age = CalculateAge(domainEvent.DateOfBirth),
            Gender = domainEvent.Gender,
            Status = "Active",
            Version = 1
        };

        await _repository.UpsertAsync(readModel, cancellationToken);
    }
}
```

### 3. Raising Domain Events in Aggregates

```csharp
public class Patient : AggregateRoot
{
    public void Create(string mrn, string firstName, string lastName, ...)
    {
        // Validate and set properties

        // Raise domain event
        var evt = new PatientCreatedEvent(Id, mrn, firstName, lastName, ...);
        RaiseEvent(evt); // or AddDomainEvent(evt)
    }
}
```

### 4. Dispatching Events After Saving

```csharp
public class CreatePatientCommandHandler
    : ICommandHandler<CreatePatientCommand, ResultDto<Guid>>
{
    private readonly ApplicationDbContext _context;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public async Task<ResultDto<Guid>> Handle(
        CreatePatientCommand request,
        CancellationToken cancellationToken)
    {
        // Create and save aggregate
        var patient = new Patient(...);
        await _context.Patients.AddAsync(patient, cancellationToken);

        // Save changes and dispatch events
        await _context.SaveChangesWithEventsAsync(
            _eventDispatcher,
            cancellationToken);

        return ResultDto<Guid>.Success(patient.Id);
    }
}
```

### 5. Querying Read Models

```csharp
public class GetPatientSummaryQueryHandler
    : IQueryHandler<GetPatientSummaryQuery, ResultDto<PatientSummaryDto>>
{
    private readonly IPatientReadModelRepository _repository;

    public async Task<ResultDto<PatientSummaryDto>> Handle(
        GetPatientSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var readModel = await _repository.GetByIdAsync(
            request.PatientId,
            cancellationToken);

        if (readModel == null)
        {
            return ResultDto<PatientSummaryDto>.Failure("Patient not found");
        }

        var dto = MapToDto(readModel);
        return ResultDto<PatientSummaryDto>.Success(dto);
    }
}
```

### 6. Rebuilding Projections

```csharp
// Rebuild a single read model
await _projectionRebuilder.RebuildReadModelAsync<PatientSummaryReadModel>(
    patientId,
    cancellationToken);

// Rebuild all read models of a type
await _projectionRebuilder.RebuildAllReadModelsAsync<PatientSummaryReadModel>(
    cancellationToken);

// Rebuild all projections
await _projectionRebuilder.RebuildAllProjectionsAsync(cancellationToken);
```

## Read Model Design Patterns

### 1. Denormalization
Read models include denormalized data to avoid joins:
```csharp
public class EncounterListReadModel
{
    public Guid PatientId { get; set; }
    public string PatientMRN { get; set; }  // Denormalized
    public string PatientName { get; set; } // Denormalized
    public string ProviderName { get; set; } // Denormalized
}
```

### 2. Calculated Fields
Pre-calculate values for query performance:
```csharp
public class PatientSummaryReadModel
{
    public int Age { get; set; } // Calculated from DateOfBirth
    public double AgeInHours { get; set; } // Pre-calculated
}
```

### 3. Search Optimization
Include search fields for full-text search:
```csharp
public class PatientSummaryReadModel
{
    public string SearchText { get; set; } // Combines multiple fields
}
```

### 4. JSON Storage
Store complex nested data as JSON:
```csharp
public class PatientDetailReadModel
{
    public List<AllergyInfo> ActiveAllergies { get; set; } // Stored as JSON
    public VitalSignsInfo? RecentVitals { get; set; } // Stored as JSON
}
```

## Database Configuration

### Connection Strings

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=emr_write;...",
    "ReadDatabase": "Host=localhost;Database=emr_read;..."
  }
}
```

### Separate Read Database Benefits
- **Performance Isolation**: Read queries don't impact write operations
- **Scaling**: Can use read replicas
- **Optimization**: Different indexes and configurations
- **Schema Independence**: Read schema can evolve independently

## Eventual Consistency

### How It Works
1. Command updates write model
2. Domain events are raised
3. Events saved to event store (future)
4. Events dispatched to projection handlers
5. Projection handlers update read models
6. Consistency manager tracks completion

### Handling Failures
- Failed projections are tracked
- Automatic retry mechanism (up to 5 times)
- Manual rebuild capability
- Monitoring via `IEventualConsistencyManager`

### Checking Consistency
```csharp
var isConsistent = await _consistencyManager
    .AreAllProjectionsCompleteAsync(eventId, cancellationToken);
```

## Performance Considerations

### Indexes
Read models have comprehensive indexes:
```csharp
entity.HasIndex(e => e.MRN).IsUnique();
entity.HasIndex(e => e.LastName);
entity.HasIndex(e => e.SearchText);
entity.HasIndex(e => new { e.ProviderId, e.ScheduleDate });
```

### No Tracking
Read context uses `AsNoTracking()` for better performance:
```csharp
options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
```

### Batch Operations
Repositories support batch upserts:
```csharp
await _repository.UpsertRangeAsync(readModels, cancellationToken);
```

## Testing

### Unit Testing Projection Handlers
```csharp
[Fact]
public async Task ProjectAsync_CreatesPatientSummary()
{
    // Arrange
    var repository = new Mock<IPatientReadModelRepository>();
    var handler = new PatientCreatedProjectionHandler(
        repository.Object,
        Mock.Of<ILogger>());

    var evt = new PatientCreatedEvent(...);

    // Act
    await handler.ProjectAsync(evt, CancellationToken.None);

    // Assert
    repository.Verify(r => r.UpsertAsync(
        It.Is<PatientSummaryReadModel>(rm => rm.Id == evt.PatientId),
        It.IsAny<CancellationToken>()));
}
```

### Integration Testing Read Models
```csharp
[Fact]
public async Task SearchAsync_FindsPatientByMRN()
{
    // Arrange
    var context = CreateInMemoryReadContext();
    var repository = new PatientReadModelRepository(context);

    // Act
    var results = await repository.SearchAsync("MRN123");

    // Assert
    Assert.Single(results);
    Assert.Equal("MRN123", results[0].MRN);
}
```

## Migration Strategy

### Initial Setup
1. Run write database migrations
2. Run read database migrations
3. Rebuild all projections from events
4. Verify consistency

### Adding New Read Models
1. Create read model entity
2. Add to `ReadDbContext`
3. Create repository interface and implementation
4. Create projection handlers
5. Register in DI
6. Run migrations
7. Rebuild from events

### Modifying Existing Read Models
1. Update read model entity
2. Update projection handlers
3. Create migration
4. Rebuild affected read models

## Future Enhancements

### 1. Event Store Integration
- Store all domain events persistently
- Enable event replay for debugging
- Support temporal queries
- Full audit trail

### 2. Caching Layer
- Add Redis cache for frequently accessed read models
- Cache invalidation on updates
- Distributed caching for scalability

### 3. Materialized Views
- Use database materialized views for complex aggregations
- Automatic refresh strategies
- Performance optimization

### 4. Read Model Snapshots
- Store snapshots of read model state
- Faster rebuilds
- Point-in-time recovery

## Troubleshooting

### Read Model Out of Sync
```csharp
// Rebuild specific read model
await _rebuilder.RebuildReadModelAsync<PatientSummaryReadModel>(
    patientId,
    cancellationToken);
```

### Failed Projections
```csharp
// Check failed projections
var failures = await _consistencyManager.GetFailedProjectionsAsync();

// Retry manually
foreach (var failure in failures)
{
    await _rebuilder.RebuildReadModelAsync<TReadModel>(
        failure.AggregateId,
        cancellationToken);
}
```

### Performance Issues
- Check indexes on read models
- Review query patterns
- Consider adding specific read models
- Use database query analysis tools

## Best Practices

1. **Keep Read Models Simple**: One read model per view/query pattern
2. **Denormalize Aggressively**: Optimize for reads, not storage
3. **Handle Missing Data**: Projection handlers should be defensive
4. **Version Read Models**: Track version for optimistic concurrency
5. **Monitor Consistency**: Track lag between write and read models
6. **Test Projections**: Ensure events correctly update read models
7. **Document Queries**: Explain what each read model optimizes for

## Summary

The CQRS Read Model Projections implementation provides:

- **5 Production-Ready Read Models**: Patient, Encounter, Order, Schedule
- **Complete Repository Layer**: Generic + specialized repositories
- **Event-Driven Updates**: Automatic projection from domain events
- **Eventual Consistency Tracking**: Monitor and retry failed projections
- **Rebuild Capability**: Reconstruct read models from events
- **Comprehensive Indexing**: Optimized for common query patterns
- **Separation of Concerns**: Independent read and write databases

This infrastructure enables the EMR system to scale reads independently, optimize queries for specific use cases, and maintain eventual consistency between command and query sides.
