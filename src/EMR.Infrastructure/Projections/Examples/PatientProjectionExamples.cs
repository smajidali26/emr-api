using EMR.Application.Common.Interfaces;
using EMR.Domain.Common;
using EMR.Domain.ReadModels;
using Microsoft.Extensions.Logging;

namespace EMR.Infrastructure.Projections.Examples;

/// <summary>
/// Example domain events for patient aggregate
/// These would be defined in EMR.Domain when patient aggregate is implemented
/// </summary>

// Example: Patient Created Event
public class PatientCreatedEvent : DomainEvent
{
    public Guid PatientId { get; }
    public string MRN { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public DateTime DateOfBirth { get; }
    public string Gender { get; }

    public PatientCreatedEvent(
        Guid patientId,
        string mrn,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string gender)
    {
        PatientId = patientId;
        MRN = mrn;
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
    }
}

// Example: Patient Contact Info Updated Event
public class PatientContactInfoUpdatedEvent : DomainEvent
{
    public Guid PatientId { get; }
    public string? PhoneNumber { get; }
    public string? Email { get; }

    public PatientContactInfoUpdatedEvent(
        Guid patientId,
        string? phoneNumber,
        string? email)
    {
        PatientId = patientId;
        PhoneNumber = phoneNumber;
        Email = email;
    }
}

/// <summary>
/// Example projection handler: Updates PatientSummaryReadModel when patient is created
/// </summary>
public class PatientCreatedProjectionHandler : ProjectionHandlerBase<PatientCreatedEvent>
{
    private readonly IPatientReadModelRepository _repository;

    public PatientCreatedProjectionHandler(
        IPatientReadModelRepository repository,
        ILogger<PatientCreatedProjectionHandler> logger)
        : base(logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    protected override async Task ProjectAsync(
        PatientCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // Create new read model from event
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
            CreatedAt = domainEvent.OccurredAt,
            Version = 1,
            SearchText = BuildSearchText(domainEvent)
        };

        // Upsert to read database
        await _repository.UpsertAsync(readModel, cancellationToken);

        Logger.LogInformation(
            "Created PatientSummaryReadModel for patient {PatientId} (MRN: {MRN})",
            domainEvent.PatientId,
            domainEvent.MRN);
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }

    private static string BuildSearchText(PatientCreatedEvent evt)
    {
        return $"{evt.MRN} {evt.FirstName} {evt.LastName} {evt.Gender}".ToLowerInvariant();
    }
}

/// <summary>
/// Example projection handler: Updates contact info in read model
/// </summary>
public class PatientContactInfoUpdatedProjectionHandler : ProjectionHandlerBase<PatientContactInfoUpdatedEvent>
{
    private readonly IPatientReadModelRepository _repository;

    public PatientContactInfoUpdatedProjectionHandler(
        IPatientReadModelRepository repository,
        ILogger<PatientContactInfoUpdatedProjectionHandler> logger)
        : base(logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    protected override async Task ProjectAsync(
        PatientContactInfoUpdatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // Get existing read model
        var readModel = await _repository.GetByIdAsync(domainEvent.PatientId, cancellationToken);

        if (readModel == null)
        {
            Logger.LogWarning(
                "PatientSummaryReadModel not found for patient {PatientId} - cannot update contact info",
                domainEvent.PatientId);
            return;
        }

        // Update contact information
        readModel.PhoneNumber = domainEvent.PhoneNumber;
        readModel.Email = domainEvent.Email;
        readModel.Version++;

        // Update search text
        var searchParts = new List<string>
        {
            readModel.MRN,
            readModel.FirstName,
            readModel.LastName,
            readModel.Gender
        };

        if (!string.IsNullOrEmpty(domainEvent.PhoneNumber))
        {
            searchParts.Add(domainEvent.PhoneNumber);
        }

        if (!string.IsNullOrEmpty(domainEvent.Email))
        {
            searchParts.Add(domainEvent.Email);
        }

        readModel.SearchText = string.Join(" ", searchParts).ToLowerInvariant();

        // Upsert to read database
        await _repository.UpsertAsync(readModel, cancellationToken);

        Logger.LogInformation(
            "Updated contact info in PatientSummaryReadModel for patient {PatientId}",
            domainEvent.PatientId);
    }
}

/// <summary>
/// Multi-projection handler: Updates both summary and detail read models
/// Demonstrates updating multiple read models from a single event
/// </summary>
public class PatientCreatedMultiProjectionHandler : ProjectionHandlerBase<PatientCreatedEvent>
{
    private readonly IPatientDetailReadModelRepository _detailRepository;

    public PatientCreatedMultiProjectionHandler(
        IPatientDetailReadModelRepository detailRepository,
        ILogger<PatientCreatedMultiProjectionHandler> logger)
        : base(logger)
    {
        _detailRepository = detailRepository ?? throw new ArgumentNullException(nameof(detailRepository));
    }

    protected override async Task ProjectAsync(
        PatientCreatedEvent domainEvent,
        CancellationToken cancellationToken)
    {
        // Create detailed read model
        var detailModel = new PatientDetailReadModel
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
            CreatedAt = domainEvent.OccurredAt,
            Version = 1,
            ActiveAllergies = new List<AllergyInfo>(),
            ActiveMedications = new List<MedicationInfo>(),
            ActiveProblems = new List<ProblemInfo>(),
            Alerts = new List<AlertInfo>()
        };

        await _detailRepository.UpsertAsync(detailModel, cancellationToken);

        Logger.LogInformation(
            "Created PatientDetailReadModel for patient {PatientId}",
            domainEvent.PatientId);
    }

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age))
        {
            age--;
        }
        return age;
    }
}
