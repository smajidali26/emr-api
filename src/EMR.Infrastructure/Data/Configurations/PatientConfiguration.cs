using EMR.Domain.Entities;
using EMR.Domain.Enums;
using EMR.Domain.ValueObjects;
using EMR.Infrastructure.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EMR.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework Core configuration for Patient entity
/// </summary>
public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        // Table name
        builder.ToTable("Patients");

        // Primary key
        builder.HasKey(p => p.Id);

        // Medical Record Number - Value object mapping
        builder.Property(p => p.MedicalRecordNumber)
            .HasConversion(
                v => v.Value,
                v => PatientIdentifier.Create(v))
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("MedicalRecordNumber");

        builder.HasIndex(p => p.MedicalRecordNumber)
            .IsUnique()
            .HasDatabaseName("IX_Patients_MRN");

        // Personal Information
        builder.Property(p => p.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.MiddleName)
            .HasMaxLength(100);

        builder.Property(p => p.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.DateOfBirth)
            .IsRequired()
            .HasColumnType("date");

        builder.HasIndex(p => p.DateOfBirth)
            .HasDatabaseName("IX_Patients_DateOfBirth");

        builder.Property(p => p.Gender)
            .IsRequired()
            .HasConversion<int>();

        // SSN - Encrypted at rest using AES-256-GCM with Azure Key Vault
        // SECURITY FIX: Task #1 - Implement SSN Encryption (Emily Wang - 16h)
        // HIPAA Compliance: PHI data encrypted at rest
        builder.Property(p => p.SocialSecurityNumber)
            .HasMaxLength(500) // Encrypted value is longer (base64 encoded: nonce + tag + ciphertext)
            .HasColumnName("SocialSecurityNumber")
            .HasConversion(new SsnEncryptionConverter());

        // Contact Information
        builder.Property(p => p.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.AlternatePhoneNumber)
            .HasMaxLength(20);

        builder.Property(p => p.Email)
            .HasMaxLength(255);

        builder.HasIndex(p => p.Email)
            .HasDatabaseName("IX_Patients_Email");

        // Address - Value object owned entity
        builder.OwnsOne(p => p.Address, address =>
        {
            address.Property(a => a.Street)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("Address_Street");

            address.Property(a => a.Street2)
                .HasMaxLength(200)
                .HasColumnName("Address_Street2");

            address.Property(a => a.City)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Address_City");

            address.Property(a => a.State)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnName("Address_State");

            address.Property(a => a.ZipCode)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("Address_ZipCode");

            address.Property(a => a.Country)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("Address_Country");
        });

        // Demographics
        builder.Property(p => p.MaritalStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.Race)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.Ethnicity)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.PreferredLanguage)
            .IsRequired()
            .HasConversion<int>();

        // Emergency Contact - Value object owned entity
        builder.OwnsOne(p => p.EmergencyContact, contact =>
        {
            contact.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("EmergencyContact_Name");

            contact.Property(c => c.Relationship)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("EmergencyContact_Relationship");

            contact.Property(c => c.PhoneNumber)
                .IsRequired()
                .HasMaxLength(20)
                .HasColumnName("EmergencyContact_PhoneNumber");

            contact.Property(c => c.AlternatePhoneNumber)
                .HasMaxLength(20)
                .HasColumnName("EmergencyContact_AlternatePhoneNumber");
        });

        // IsActive - Required, indexed for performance
        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_Patients_IsActive");

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(255);

        // Soft delete fields
        builder.Property(p => p.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(p => p.IsDeleted)
            .HasDatabaseName("IX_Patients_IsDeleted");

        builder.Property(p => p.DeletedAt)
            .IsRequired(false);

        builder.Property(p => p.DeletedBy)
            .HasMaxLength(255);

        // Row version for concurrency control
        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        // Ignore computed properties
        builder.Ignore(p => p.FullName);
        builder.Ignore(p => p.Age);

        // Index for common searches
        builder.HasIndex(p => new { p.LastName, p.FirstName })
            .HasDatabaseName("IX_Patients_Name");
    }
}
