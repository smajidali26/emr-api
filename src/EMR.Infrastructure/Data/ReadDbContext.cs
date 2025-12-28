using EMR.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace EMR.Infrastructure.Data;

/// <summary>
/// Database context for read models (CQRS read side)
/// Optimized for query performance with denormalized data
/// </summary>
public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Patient summary read models
    /// </summary>
    public DbSet<PatientSummaryReadModel> PatientSummaries => Set<PatientSummaryReadModel>();

    /// <summary>
    /// Patient detail read models
    /// </summary>
    public DbSet<PatientDetailReadModel> PatientDetails => Set<PatientDetailReadModel>();

    /// <summary>
    /// Encounter list read models
    /// </summary>
    public DbSet<EncounterListReadModel> EncounterList => Set<EncounterListReadModel>();

    /// <summary>
    /// Active orders read models
    /// </summary>
    public DbSet<ActiveOrdersReadModel> ActiveOrders => Set<ActiveOrdersReadModel>();

    /// <summary>
    /// Provider schedule read models
    /// </summary>
    public DbSet<ProviderScheduleReadModel> ProviderSchedules => Set<ProviderScheduleReadModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema for read models
        modelBuilder.HasDefaultSchema("read");

        ConfigurePatientSummary(modelBuilder);
        ConfigurePatientDetail(modelBuilder);
        ConfigureEncounterList(modelBuilder);
        ConfigureActiveOrders(modelBuilder);
        ConfigureProviderSchedule(modelBuilder);
    }

    private static void ConfigurePatientSummary(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientSummaryReadModel>(entity =>
        {
            entity.ToTable("PatientSummaries");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.MRN).IsUnique();
            entity.HasIndex(e => e.FullName);
            entity.HasIndex(e => e.LastName);
            entity.HasIndex(e => e.DateOfBirth);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PrimaryCareProviderId);
            entity.HasIndex(e => e.SearchText); // Full-text search
            entity.HasIndex(e => e.LastUpdatedAt);

            entity.Property(e => e.MRN).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PrimaryCareProvider).HasMaxLength(250);
            entity.Property(e => e.FullAddress).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.PrimaryInsurance).HasMaxLength(250);
            entity.Property(e => e.SearchText).HasMaxLength(2000);
        });
    }

    private static void ConfigurePatientDetail(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PatientDetailReadModel>(entity =>
        {
            entity.ToTable("PatientDetails");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.MRN).IsUnique();
            entity.HasIndex(e => e.LastUpdatedAt);

            entity.Property(e => e.MRN).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.MiddleName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.PreferredName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(50);
            entity.Property(e => e.BiologicalSex).HasMaxLength(50);
            entity.Property(e => e.PreferredPronouns).HasMaxLength(50);
            entity.Property(e => e.SSN).HasMaxLength(50); // Encrypted
            entity.Property(e => e.PrimaryPhone).HasMaxLength(20);
            entity.Property(e => e.SecondaryPhone).HasMaxLength(20);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.PreferredContactMethod).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PreferredLanguage).HasMaxLength(50);
            entity.Property(e => e.Race).HasMaxLength(100);
            entity.Property(e => e.Ethnicity).HasMaxLength(100);
            entity.Property(e => e.MaritalStatus).HasMaxLength(50);
            entity.Property(e => e.BloodType).HasMaxLength(10);

            // JSON columns for complex objects
            entity.OwnsOne(e => e.PrimaryCareProvider, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsOne(e => e.Address, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsOne(e => e.EmergencyContact, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsOne(e => e.PrimaryInsurance, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsOne(e => e.SecondaryInsurance, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsOne(e => e.RecentVitals, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsOne(e => e.LastVisit, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsOne(e => e.NextAppointment, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsMany(e => e.ActiveAllergies, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsMany(e => e.ActiveMedications, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsMany(e => e.ActiveProblems, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsMany(e => e.Alerts, nav =>
            {
                nav.ToJson();
            });
        });
    }

    private static void ConfigureEncounterList(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EncounterListReadModel>(entity =>
        {
            entity.ToTable("EncounterList");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.EncounterNumber).IsUnique();
            entity.HasIndex(e => e.ProviderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.SearchText);
            entity.HasIndex(e => e.LastUpdatedAt);

            entity.Property(e => e.PatientMRN).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PatientName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.EncounterNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.EncounterType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.ProviderSpecialty).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Facility).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ChiefComplaint).HasMaxLength(500).IsRequired();
            entity.Property(e => e.VisitReason).HasMaxLength(500);
            entity.Property(e => e.PrimaryDiagnosisCode).HasMaxLength(50);
            entity.Property(e => e.PrimaryDiagnosisDescription).HasMaxLength(500);
            entity.Property(e => e.AdmissionType).HasMaxLength(100);
            entity.Property(e => e.DischargeDisposition).HasMaxLength(100);
            entity.Property(e => e.AuthorizationNumber).HasMaxLength(50);
            entity.Property(e => e.BillingStatus).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SearchText).HasMaxLength(2000);
        });
    }

    private static void ConfigureActiveOrders(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActiveOrdersReadModel>(entity =>
        {
            entity.ToTable("ActiveOrders");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.PatientId);
            entity.HasIndex(e => e.EncounterId);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.OrderType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.OrderingProviderId);
            entity.HasIndex(e => e.PerformingDepartment);
            entity.HasIndex(e => e.AssignedToId);
            entity.HasIndex(e => e.OrderedAt);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasIndex(e => e.IsCritical);
            entity.HasIndex(e => e.IsOverdue);
            entity.HasIndex(e => e.SearchText);
            entity.HasIndex(e => e.LastUpdatedAt);

            entity.Property(e => e.PatientMRN).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PatientName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.PatientGender).HasMaxLength(50);
            entity.Property(e => e.EncounterNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OrderType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OrderCategory).HasMaxLength(100).IsRequired();
            entity.Property(e => e.OrderDescription).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Priority).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OrderingProviderName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.PerformingDepartment).HasMaxLength(100);
            entity.Property(e => e.PerformingLocation).HasMaxLength(100);
            entity.Property(e => e.AssignedToName).HasMaxLength(250);
            entity.Property(e => e.ClinicalIndication).HasMaxLength(1000);
            entity.Property(e => e.SpecialInstructions).HasMaxLength(1000);
            entity.Property(e => e.AuthorizationStatus).HasMaxLength(50);
            entity.Property(e => e.AuthorizationNumber).HasMaxLength(50);
            entity.Property(e => e.ResultsStatus).HasMaxLength(50);
            entity.Property(e => e.PatientLocation).HasMaxLength(100);
            entity.Property(e => e.PatientRoom).HasMaxLength(50);
            entity.Property(e => e.SearchText).HasMaxLength(2000);

            // JSON column for alert flags
            entity.Property(e => e.AlertFlags)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });
    }

    private static void ConfigureProviderSchedule(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProviderScheduleReadModel>(entity =>
        {
            entity.ToTable("ProviderSchedules");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.ProviderId, e.ScheduleDate }).IsUnique();
            entity.HasIndex(e => e.ScheduleDate);
            entity.HasIndex(e => e.Department);
            entity.HasIndex(e => e.Specialty);
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.IsOnCall);
            entity.HasIndex(e => e.AcceptingNewPatients);
            entity.HasIndex(e => e.LastUpdatedAt);

            entity.Property(e => e.ProviderName).HasMaxLength(250).IsRequired();
            entity.Property(e => e.Specialty).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Department).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DayOfWeek).HasMaxLength(20).IsRequired();
            entity.Property(e => e.Location).HasMaxLength(100).IsRequired();
            entity.Property(e => e.RoomNumber).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(1000);
            entity.Property(e => e.OverrideReason).HasMaxLength(500);

            // JSON columns for complex objects
            entity.OwnsMany(e => e.AvailableSlots, nav =>
            {
                nav.ToJson();
            });

            entity.OwnsMany(e => e.BookedAppointments, nav =>
            {
                nav.ToJson();
            });

            entity.Property(e => e.AppointmentTypes)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });
    }

    /// <summary>
    /// Optimized save for read models (no tracking or events)
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update LastUpdatedAt for modified read models
        var entries = ChangeTracker.Entries<BaseReadModel>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.LastUpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
