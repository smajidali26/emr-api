namespace EMR.Domain.ReadModels;

/// <summary>
/// Read model optimized for provider scheduling and availability
/// Denormalized for query performance
/// </summary>
public class ProviderScheduleReadModel : BaseReadModel
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public Guid ProviderId { get; set; }

    /// <summary>
    /// Provider name (denormalized)
    /// </summary>
    public string ProviderName { get; set; } = string.Empty;

    /// <summary>
    /// Provider specialty
    /// </summary>
    public string Specialty { get; set; } = string.Empty;

    /// <summary>
    /// Provider department
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Schedule date
    /// </summary>
    public DateTime ScheduleDate { get; set; }

    /// <summary>
    /// Day of week
    /// </summary>
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>
    /// Shift start time
    /// </summary>
    public TimeSpan ShiftStartTime { get; set; }

    /// <summary>
    /// Shift end time
    /// </summary>
    public TimeSpan ShiftEndTime { get; set; }

    /// <summary>
    /// Total shift duration in hours
    /// </summary>
    public double ShiftDurationHours { get; set; }

    /// <summary>
    /// Location/Facility
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Room/Office number
    /// </summary>
    public string? RoomNumber { get; set; }

    /// <summary>
    /// Available appointment slots
    /// </summary>
    public List<AppointmentSlot> AvailableSlots { get; set; } = new();

    /// <summary>
    /// Booked appointments
    /// </summary>
    public List<BookedAppointment> BookedAppointments { get; set; } = new();

    /// <summary>
    /// Total appointment slots for the day
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// Number of available slots
    /// </summary>
    public int AvailableSlotsCount { get; set; }

    /// <summary>
    /// Number of booked slots
    /// </summary>
    public int BookedSlotsCount { get; set; }

    /// <summary>
    /// Number of blocked/unavailable slots
    /// </summary>
    public int BlockedSlotsCount { get; set; }

    /// <summary>
    /// Utilization percentage
    /// </summary>
    public double UtilizationPercentage { get; set; }

    /// <summary>
    /// Appointment types accepted
    /// </summary>
    public List<string> AppointmentTypes { get; set; } = new();

    /// <summary>
    /// Average appointment duration in minutes
    /// </summary>
    public int AverageAppointmentDuration { get; set; }

    /// <summary>
    /// Is on call
    /// </summary>
    public bool IsOnCall { get; set; }

    /// <summary>
    /// Is virtual/telehealth available
    /// </summary>
    public bool IsVirtualAvailable { get; set; }

    /// <summary>
    /// Accepting new patients
    /// </summary>
    public bool AcceptingNewPatients { get; set; }

    /// <summary>
    /// Schedule notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Override/Exception flag
    /// </summary>
    public bool HasScheduleOverride { get; set; }

    /// <summary>
    /// Override reason
    /// </summary>
    public string? OverrideReason { get; set; }

    /// <summary>
    /// When the schedule was last updated
    /// </summary>
    public DateTime ScheduleUpdatedAt { get; set; }
}

#region Nested Classes

public class AppointmentSlot
{
    public Guid Id { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string SlotType { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string? BlockReason { get; set; }
}

public class BookedAppointment
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientMRN { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string AppointmentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public bool IsVirtual { get; set; }
    public bool IsNewPatient { get; set; }
    public bool HasArrived { get; set; }
    public DateTime? CheckInTime { get; set; }
}

#endregion
