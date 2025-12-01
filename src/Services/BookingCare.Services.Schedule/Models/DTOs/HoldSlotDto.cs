using System.ComponentModel.DataAnnotations;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Models.DTOs;

/// <summary>
/// DTO for hold slot information
/// </summary>
public class HoldSlotDto
{
    /// <summary>
    /// Target ID (can be DoctorId or ServiceMedicalId based on TargetType)
    /// </summary>
    public Guid TargetId { get; set; }

    /// <summary>
    /// Type of target (Doctor or ServiceMedical)
    /// </summary>
    public HoldSlotTargetType TargetType { get; set; } = HoldSlotTargetType.Doctor;

    public DateOnly Date { get; set; }
    public AppointmentTime AppointmentTimeId { get; set; }
    public Guid UserId { get; set; }
    public DateTime HeldAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int RemainingSeconds { get; set; }
}

/// <summary>
/// Response for hold slot operation
/// </summary>
public class HoldSlotResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public HoldSlotDto? HoldSlot { get; set; }
    public int RemainingSeconds { get; set; }
}

/// <summary>
/// Request to hold a slot
/// </summary>
public class HoldSlotRequest
{
    /// <summary>
    /// Target ID (can be DoctorId or ServiceMedicalId based on TargetType)
    /// </summary>
    [Required]
    public required Guid TargetId { get; set; }

    /// <summary>
    /// Type of target (Doctor or ServiceMedical). Defaults to Doctor for backward compatibility.
    /// </summary>
    public HoldSlotTargetType TargetType { get; set; } = HoldSlotTargetType.Doctor;

    [Required]
    public required DateOnly Date { get; set; }

    [Required]
    public required AppointmentTime AppointmentTimeId { get; set; }
}

/// <summary>
/// Request to release a held slot
/// </summary>
public class ReleaseSlotRequest
{
    /// <summary>
    /// Target ID (can be DoctorId or ServiceMedicalId based on TargetType)
    /// </summary>
    [Required]
    public required Guid TargetId { get; set; }

    /// <summary>
    /// Type of target (Doctor or ServiceMedical). Defaults to Doctor for backward compatibility.
    /// </summary>
    public HoldSlotTargetType TargetType { get; set; } = HoldSlotTargetType.Doctor;

    [Required]
    public required DateOnly Date { get; set; }

    [Required]
    public required AppointmentTime AppointmentTimeId { get; set; }
}

#region Specialty Hold Slot DTOs (for "hospital assigns doctor" mode)

/// <summary>
/// Request to hold a specialty slot (capacity-based holding)
/// </summary>
public class HoldSpecialtySlotRequest
{
    [Required]
    public required Guid HospitalId { get; set; }

    [Required]
    public required Guid SpecialtyId { get; set; }

    [Required]
    public required DateOnly Date { get; set; }

    [Required]
    public required AppointmentTime AppointmentTimeId { get; set; }

    /// <summary>
    /// Maximum capacity for this slot (number of available doctors)
    /// This is used to validate if the slot can still be held
    /// </summary>
    [Required]
    public required int MaxCapacity { get; set; }
}

/// <summary>
/// Request to release a held specialty slot
/// </summary>
public class ReleaseSpecialtySlotRequest
{
    [Required]
    public required Guid HospitalId { get; set; }

    [Required]
    public required Guid SpecialtyId { get; set; }

    [Required]
    public required DateOnly Date { get; set; }

    [Required]
    public required AppointmentTime AppointmentTimeId { get; set; }
}

#endregion
