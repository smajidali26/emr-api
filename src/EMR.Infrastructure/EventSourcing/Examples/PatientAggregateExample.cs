using EMR.Domain.Common;
using EMR.Domain.Events.Patient;

namespace EMR.Infrastructure.EventSourcing.Examples;

/// <summary>
/// Example implementation of an event-sourced Patient aggregate.
/// This demonstrates the proper pattern for implementing event sourcing with aggregates.
/// </summary>
public class PatientAggregate : AggregateRoot
{
    // Aggregate state - private setters, updated only through events
    public string MedicalRecordNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public string Gender { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Address { get; private set; }

    // Required parameterless constructor for event replay
    public PatientAggregate()
    {
    }

    /// <summary>
    /// Factory method to register a new patient.
    /// This is the entry point for creating a new aggregate.
    /// </summary>
    public static PatientAggregate Register(
        string medicalRecordNumber,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string gender,
        string? email,
        string? phoneNumber,
        string userId,
        string? correlationId = null)
    {
        // Validate business rules
        if (string.IsNullOrWhiteSpace(medicalRecordNumber))
            throw new ArgumentException("Medical record number is required", nameof(medicalRecordNumber));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        // Create aggregate and raise event
        var patient = new PatientAggregate();
        var patientId = Guid.NewGuid();

        var @event = new PatientRegisteredEvent(
            patientId,
            medicalRecordNumber,
            firstName,
            lastName,
            dateOfBirth,
            gender,
            userId,
            correlationId)
        {
            Email = email,
            PhoneNumber = phoneNumber
        };

        // RaiseEvent both applies the event AND adds it to uncommitted events
        patient.RaiseEvent(@event);

        return patient;
    }

    /// <summary>
    /// Updates patient demographics.
    /// This is a command method that creates and raises an event.
    /// </summary>
    public void UpdateDemographics(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? phoneNumber = null,
        string? address = null,
        string userId = "",
        string? updateReason = null,
        string? correlationId = null,
        string? causationId = null)
    {
        // Check if anything actually changed
        var hasChanges =
            (firstName != null && firstName != FirstName) ||
            (lastName != null && lastName != LastName) ||
            (email != null && email != Email) ||
            (phoneNumber != null && phoneNumber != PhoneNumber) ||
            (address != null && address != Address);

        if (!hasChanges)
        {
            return; // No changes, don't create an event
        }

        // Build previous values for audit
        var previousValues = new Dictionary<string, string>();
        if (firstName != null && firstName != FirstName)
            previousValues["FirstName"] = FirstName;
        if (lastName != null && lastName != LastName)
            previousValues["LastName"] = LastName;

        // Create and raise event
        var @event = new PatientDemographicsUpdatedEvent(
            Id,
            userId,
            correlationId,
            causationId)
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = phoneNumber,
            Address = address,
            PreviousValues = previousValues,
            UpdateReason = updateReason
        };

        RaiseEvent(@event);
    }

    /// <summary>
    /// Applies events to the aggregate state.
    /// This method is called both when raising new events and when replaying historical events.
    /// IMPORTANT: This method should ONLY update state, no business logic!
    /// </summary>
    protected override void ApplyEvent(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case PatientRegisteredEvent e:
                ApplyPatientRegistered(e);
                break;

            case PatientDemographicsUpdatedEvent e:
                ApplyPatientDemographicsUpdated(e);
                break;

            default:
                // Unknown event type - log warning but don't throw
                // This allows for graceful handling of deprecated events
                break;
        }

        base.ApplyEvent(domainEvent);
    }

    private void ApplyPatientRegistered(PatientRegisteredEvent e)
    {
        // Set aggregate ID from event
        Id = e.PatientId;

        // Update state from event
        MedicalRecordNumber = e.MedicalRecordNumber;
        FirstName = e.FirstName;
        LastName = e.LastName;
        DateOfBirth = e.DateOfBirth;
        Gender = e.Gender;
        Email = e.Email;
        PhoneNumber = e.PhoneNumber;
        Address = e.Address;

        // Set audit fields
        CreatedAt = e.OccurredAt;
        CreatedBy = e.UserId ?? "System";
    }

    private void ApplyPatientDemographicsUpdated(PatientDemographicsUpdatedEvent e)
    {
        // Update only changed fields
        if (e.FirstName != null)
            FirstName = e.FirstName;

        if (e.LastName != null)
            LastName = e.LastName;

        if (e.Email != null)
            Email = e.Email;

        if (e.PhoneNumber != null)
            PhoneNumber = e.PhoneNumber;

        if (e.Address != null)
            Address = e.Address;

        // Update audit fields
        UpdatedAt = e.OccurredAt;
        UpdatedBy = e.UserId ?? "System";
    }

    /// <summary>
    /// Example business rule - validates that the patient is old enough for a specific procedure.
    /// This is NOT an event - it's a business rule check.
    /// </summary>
    public bool IsOldEnoughFor(string procedureName, int minimumAge)
    {
        var age = CalculateAge();
        return age >= minimumAge;
    }

    private int CalculateAge()
    {
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > today.AddYears(-age))
            age--;
        return age;
    }
}

/// <summary>
/// Example usage of the event-sourced Patient aggregate.
/// This shows how to use the EventSourcedRepository with the aggregate.
/// </summary>
public class PatientAggregateUsageExample
{
    private readonly EventSourcedRepository<PatientAggregate> _repository;

    public PatientAggregateUsageExample(EventSourcedRepository<PatientAggregate> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Example: Register a new patient
    /// </summary>
    public async Task<Guid> RegisterNewPatientAsync(
        string mrn,
        string firstName,
        string lastName,
        DateTime dateOfBirth,
        string gender,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Create aggregate using factory method
        var patient = PatientAggregate.Register(
            mrn,
            firstName,
            lastName,
            dateOfBirth,
            gender,
            null, // email
            null, // phone
            userId,
            Guid.NewGuid().ToString()); // correlation ID

        // Save to event store
        await _repository.SaveAsync(patient, cancellationToken);

        return patient.Id;
    }

    /// <summary>
    /// Example: Update patient information
    /// </summary>
    public async Task UpdatePatientInformationAsync(
        Guid patientId,
        string? newEmail,
        string? newPhone,
        string userId,
        CancellationToken cancellationToken = default)
    {
        // Load aggregate from event store
        var patient = await _repository.GetByIdAsync(patientId, cancellationToken);
        if (patient == null)
        {
            throw new InvalidOperationException($"Patient {patientId} not found");
        }

        // Execute command
        patient.UpdateDemographics(
            email: newEmail,
            phoneNumber: newPhone,
            userId: userId,
            updateReason: "Contact information update");

        // Save changes (new events) to event store
        await _repository.SaveAsync(patient, cancellationToken);
    }

    /// <summary>
    /// Example: Check if patient exists
    /// </summary>
    public async Task<bool> PatientExistsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return await _repository.ExistsAsync(patientId, cancellationToken);
    }
}
