using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Schedule.Models.DTOs;

/// <summary>
/// DTO for hold slot information
/// </summary>
public class HoldSlotDto
{
    public Guid DoctorId { get; set; }
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
    public Guid DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public AppointmentTime AppointmentTimeId { get; set; }
}

/// <summary>
/// Request to release a held slot
/// </summary>
public class ReleaseSlotRequest
{
    public Guid DoctorId { get; set; }
    public DateOnly Date { get; set; }
    public AppointmentTime AppointmentTimeId { get; set; }
}
