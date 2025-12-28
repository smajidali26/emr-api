# CQRS Read Models - Quick Start Guide

## 5-Minute Setup

### 1. Configure Services (Program.cs)
```csharp
using EMR.Infrastructure.Configuration;

// Add CQRS read model services
builder.Services.AddCqrsReadModels(builder.Configuration);

// Initialize read database after building
var app = builder.Build();
await app.Services.InitializeReadDatabaseAsync();
```

### 2. Connection String (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=emr;...",
    "ReadDatabase": "Host=localhost;Database=emr_read;..."
  }
}
```

### 3. Create a Projection Handler
```csharp
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
        PatientCreatedEvent evt,
        CancellationToken cancellationToken)
    {
        var readModel = new PatientSummaryReadModel
        {
            Id = evt.PatientId,
            MRN = evt.MRN,
            FirstName = evt.FirstName,
            LastName = evt.LastName,
            FullName = $"{evt.FirstName} {evt.LastName}",
            DateOfBirth = evt.DateOfBirth,
            Age = CalculateAge(evt.DateOfBirth),
            Gender = evt.Gender,
            Status = "Active"
        };

        await _repository.UpsertAsync(readModel, cancellationToken);
    }
}
```

### 4. Dispatch Events in Command Handler
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
        var patient = new Patient(...);
        await _context.Patients.AddAsync(patient, cancellationToken);

        // Save and dispatch events
        await _context.SaveChangesWithEventsAsync(
            _eventDispatcher,
            cancellationToken);

        return ResultDto<Guid>.Success(patient.Id);
    }
}
```

### 5. Query Read Models
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
            return ResultDto<PatientSummaryDto>.Failure("Not found");

        var dto = MapToDto(readModel);
        return ResultDto<PatientSummaryDto>.Success(dto);
    }
}
```

## Common Queries

### Search Patients
```csharp
var patients = await _patientRepository.SearchAsync("john", maxResults: 50);
```

### Get Active Orders
```csharp
var orders = await _orderRepository.GetActiveByPatientAsync(patientId);
```

### Get Urgent Orders
```csharp
var urgentOrders = await _orderRepository.GetUrgentOrdersAsync();
```

### Get Provider Schedule
```csharp
var schedule = await _scheduleRepository.GetByProviderAndDateAsync(
    providerId,
    DateTime.Today);
```

### Search with Pagination
```csharp
var (items, totalCount) = await _repository.GetPagedAsync(
    pageNumber: 1,
    pageSize: 20,
    predicate: p => p.Status == "Active",
    orderBy: p => p.LastName,
    ascending: true);
```

## Rebuild Projections

### Rebuild Single Read Model
```csharp
await _projectionRebuilder.RebuildReadModelAsync<PatientSummaryReadModel>(
    patientId);
```

### Rebuild All of Type
```csharp
await _projectionRebuilder.RebuildAllReadModelsAsync<PatientSummaryReadModel>();
```

### Rebuild Everything
```csharp
await _projectionRebuilder.RebuildAllProjectionsAsync();
```

## Key Concepts

### Write Side vs Read Side
- **Write Side**: Commands modify aggregates, raise events
- **Read Side**: Events update denormalized read models

### Eventual Consistency
- Write happens immediately
- Read models updated asynchronously via events
- Usually milliseconds, but not guaranteed

### When to Use Each Read Model
- **PatientSummaryReadModel**: Lists, search, quick lookups
- **PatientDetailReadModel**: Full patient details view
- **EncounterListReadModel**: Encounter history
- **ActiveOrdersReadModel**: Worklists, dashboards
- **ProviderScheduleReadModel**: Scheduling, availability

## Best Practices

1. One read model per use case/view
2. Denormalize aggressively - optimize for reads
3. Pre-calculate values (age, counts, flags)
4. Use search fields for full-text search
5. Handle missing data gracefully in projections
6. Monitor projection lag
7. Test projection handlers thoroughly

## Troubleshooting

### Read Model Out of Sync
```csharp
await _projectionRebuilder.RebuildReadModelAsync<T>(aggregateId);
```

### Check Failed Projections
```csharp
var failures = await _consistencyManager.GetFailedProjectionsAsync();
```

### Verify Consistency
```csharp
var isComplete = await _consistencyManager.AreAllProjectionsCompleteAsync(eventId);
```

## Next Steps

1. See `CQRS_READ_MODELS.md` for comprehensive guide
2. See `FEATURE_56_IMPLEMENTATION_SUMMARY.md` for architecture details
3. Review example projection handlers in `Projections/Examples/`
4. Add domain events to your aggregates
5. Create projection handlers for your events
6. Query read models in your query handlers

## File Locations

- **Read Models**: `EMR.Domain/ReadModels/`
- **Interfaces**: `EMR.Application/Common/Interfaces/`
- **Repositories**: `EMR.Infrastructure/Repositories/ReadModels/`
- **Projections**: `EMR.Infrastructure/Projections/`
- **Config**: `EMR.Infrastructure/Configuration/CqrsConfiguration.cs`
