using EMR.Domain.Common;
using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.Events.Patient;
using EMR.Domain.ValueObjects;
using FluentAssertions;

namespace EMR.UnitTests.Domain.Events;

/// <summary>
/// Unit tests for PatientRegisteredEvent domain event.
/// QA Condition: Verify event is raised with correct properties when Patient is created.
/// </summary>
public class PatientRegisteredEventTests
{
    private readonly PatientAddress _validAddress;
    private readonly EmergencyContact _validEmergencyContact;
    private const string CreatedBy = "test-user";

    public PatientRegisteredEventTests()
    {
        _validAddress = PatientAddress.Create(
            "123 Main St",
            null,
            "Springfield",
            "IL",
            "62701",
            "USA");

        _validEmergencyContact = EmergencyContact.Create(
            "Jane Doe",
            "Spouse",
            "555-1234");
    }

    #region Event Raising Tests

    [Fact]
    public void Constructor_ShouldRaisePatientRegisteredEvent()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        patient.DomainEvents.Should().HaveCount(1);
        patient.DomainEvents.First().Should().BeOfType<PatientRegisteredEvent>();
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithCorrectPatientId()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.PatientId.Should().Be(patient.Id);
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithCorrectMRN()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.MedicalRecordNumber.Should().Be(patient.MedicalRecordNumber.Value);
        domainEvent.MedicalRecordNumber.Should().StartWith("MRN-");
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithCorrectFirstName()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.FirstName.Should().Be("John");
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithCorrectLastName()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithCorrectDateOfBirth()
    {
        // Arrange
        var dateOfBirth = new DateTime(1990, 5, 15);

        // Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: dateOfBirth,
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.DateOfBirth.Should().Be(dateOfBirth.Date);
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithCorrectGender()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.Gender.Should().Be(Gender.Male.ToString());
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithCorrectUserId()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.UserId.Should().Be(CreatedBy);
    }

    [Theory]
    [InlineData(Gender.Male)]
    [InlineData(Gender.Female)]
    [InlineData(Gender.Unknown)]
    [InlineData(Gender.Other)]
    public void Constructor_ShouldRaiseEventWithAnyValidGender(Gender gender)
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "Alex",
            lastName: "Smith",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: gender,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.Gender.Should().Be(gender.ToString());
    }

    #endregion

    #region Event Data Integrity Tests

    [Fact]
    public void Constructor_WithTrimmedNames_ShouldRaiseEventWithTrimmedValues()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "  John  ",
            lastName: "  Doe  ",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.FirstName.Should().Be("John");
        domainEvent.LastName.Should().Be("Doe");
    }

    [Fact]
    public void Constructor_ShouldRaiseEventWithTimestamp()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        var afterCreation = DateTime.UtcNow;

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.OccurredAt.Should().BeOnOrAfter(beforeCreation);
        domainEvent.OccurredAt.Should().BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Constructor_MultiplePatients_ShouldRaiseUniqueEvents()
    {
        // Arrange & Act
        var patient1 = new Patient("John", "Doe", new DateTime(1990, 5, 15), Gender.Male, "555-1234", _validAddress, _validEmergencyContact, CreatedBy);
        var patient2 = new Patient("Jane", "Smith", new DateTime(1985, 3, 20), Gender.Female, "555-5678", _validAddress, _validEmergencyContact, CreatedBy);

        // Assert
        var event1 = patient1.DomainEvents.First() as PatientRegisteredEvent;
        var event2 = patient2.DomainEvents.First() as PatientRegisteredEvent;

        event1.Should().NotBeNull();
        event2.Should().NotBeNull();
        event1!.PatientId.Should().NotBe(event2!.PatientId);
        event1.MedicalRecordNumber.Should().NotBe(event2.MedicalRecordNumber);
        event1.EventId.Should().NotBe(event2.EventId);
    }

    #endregion

    #region AggregateRoot Version Tests

    [Fact]
    public void Constructor_ShouldIncrementVersion()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert - Version should be 1 after first event
        patient.Version.Should().Be(1);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Act
        patient.ClearDomainEvents();

        // Assert
        patient.DomainEvents.Should().BeEmpty();
        patient.Version.Should().Be(1); // Version should remain
    }

    #endregion

    #region Event Base Class Tests

    [Fact]
    public void PatientRegisteredEvent_ShouldHaveUniqueEventId()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First() as PatientRegisteredEvent;
        domainEvent.Should().NotBeNull();
        domainEvent!.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void PatientRegisteredEvent_ShouldImplementIDomainEvent()
    {
        // Arrange & Act
        var patient = new Patient(
            firstName: "John",
            lastName: "Doe",
            dateOfBirth: new DateTime(1990, 5, 15),
            gender: Gender.Male,
            phoneNumber: "555-1234",
            address: _validAddress,
            emergencyContact: _validEmergencyContact,
            createdBy: CreatedBy);

        // Assert
        var domainEvent = patient.DomainEvents.First();
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }

    #endregion
}
