using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Schedule.Enums;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// Interface for managing slot hold operations
/// </summary>
public interface IHoldSlotService
{
    /// <summary>
    /// Hold a slot for a specific user for 5 minutes
    /// </summary>
    /// <param name="request">Hold slot request</param>
    /// <param name="userId">User ID who is holding the slot</param>
    /// <returns>Hold slot response with remaining time</returns>
    Task<HoldSlotResponse> HoldSlotAsync(HoldSlotRequest request, Guid userId);

    /// <summary>
    /// Release a held slot
    /// </summary>
    /// <param name="request">Release slot request</param>
    /// <param name="userId">User ID who is releasing the slot</param>
    /// <returns>True if successfully released</returns>
    Task<bool> ReleaseSlotAsync(ReleaseSlotRequest request, Guid userId);

    /// <summary>
    /// Get held slots for a specific target (doctor or service medical) and date (excluding current user)
    /// </summary>
    /// <param name="targetId">Target ID (Doctor or ServiceMedical)</param>
    /// <param name="targetType">Type of target</param>
    /// <param name="date">Date</param>
    /// <param name="currentUserId">Current user ID to exclude from results</param>
    /// <returns>List of held appointment time IDs</returns>
    Task<List<AppointmentTime>> GetHeldSlotsAsync(Guid targetId, HoldSlotTargetType targetType, DateOnly date, Guid currentUserId);

    /// <summary>
    /// Get held slots for a specific doctor and date (excluding current user) - backward compatible
    /// </summary>
    Task<List<AppointmentTime>> GetHeldSlotsAsync(Guid doctorId, DateOnly date, Guid currentUserId);

    /// <summary>
    /// Check if a specific slot is held by another user
    /// </summary>
    /// <param name="targetId">Target ID (Doctor or ServiceMedical)</param>
    /// <param name="targetType">Type of target</param>
    /// <param name="date">Date</param>
    /// <param name="appointmentTimeId">Appointment time ID</param>
    /// <param name="currentUserId">Current user ID</param>
    /// <returns>True if slot is held by another user</returns>
    Task<bool> IsSlotHeldByOtherUserAsync(Guid targetId, HoldSlotTargetType targetType, DateOnly date, AppointmentTime appointmentTimeId, Guid currentUserId);

    /// <summary>
    /// Check if a specific slot is held by another user - backward compatible
    /// </summary>
    Task<bool> IsSlotHeldByOtherUserAsync(Guid doctorId, DateOnly date, AppointmentTime appointmentTimeId, Guid currentUserId);

    /// <summary>
    /// Release all held slots for a user (cleanup when user leaves)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Number of slots released</returns>
    Task<int> ReleaseAllUserSlotsAsync(Guid userId);

    /// <summary>
    /// Get remaining time for a held slot
    /// </summary>
    /// <param name="targetId">Target ID (Doctor or ServiceMedical)</param>
    /// <param name="targetType">Type of target</param>
    /// <param name="date">Date</param>
    /// <param name="appointmentTimeId">Appointment time ID</param>
    /// <param name="userId">User ID</param>
    /// <returns>Remaining seconds or 0 if not held</returns>
    Task<int> GetRemainingTimeAsync(Guid targetId, HoldSlotTargetType targetType, DateOnly date, AppointmentTime appointmentTimeId, Guid userId);

    /// <summary>
    /// Get remaining time for a held slot - backward compatible
    /// </summary>
    Task<int> GetRemainingTimeAsync(Guid doctorId, DateOnly date, AppointmentTime appointmentTimeId, Guid userId);
}
