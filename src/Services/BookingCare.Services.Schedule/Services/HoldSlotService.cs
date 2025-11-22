using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// Service for managing slot hold operations
/// </summary>
public class HoldSlotService : BaseService, IHoldSlotService
{
    private readonly ICacheService _cacheService;
    private const int HOLD_DURATION_MINUTES = 5;
    private const string DateFormat = "yyyy-MM-dd";

    public HoldSlotService(
        ICacheService cacheService,
        ILogger<HoldSlotService> logger) : base(logger)
    {
        _cacheService = cacheService;
    }

    public async Task<HoldSlotResponse> HoldSlotAsync(HoldSlotRequest request, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Attempting to hold slot for doctor {DoctorId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null, request.DoctorId, request.Date, request.AppointmentTimeId, userId);

            // Check if slot is already held by another user
            var isHeldByOther = await IsSlotHeldByOtherUserAsync(
                request.DoctorId, request.Date, request.AppointmentTimeId, userId);

            if (isHeldByOther)
            {
                LogWarning("Slot already held by another user: {DoctorId}:{Date}:{AppointmentTimeId}",
                    null, request.DoctorId, request.Date, request.AppointmentTimeId);

                return new HoldSlotResponse
                {
                    Success = false,
                    Message = "Khung giờ này đã được người khác chọn. Vui lòng chọn khung giờ khác.",
                    RemainingSeconds = 0
                };
            }

            // Release any existing held slots for this user (user can only hold one slot at a time)
            await ReleaseAllUserSlotsAsync(userId);

            // Create hold slot data
            var heldAt = DateTime.UtcNow;
            var expiresAt = heldAt.AddMinutes(HOLD_DURATION_MINUTES);
            var holdSlot = new HoldSlotDto
            {
                DoctorId = request.DoctorId,
                Date = request.Date,
                AppointmentTimeId = request.AppointmentTimeId,
                UserId = userId,
                HeldAt = heldAt,
                ExpiresAt = expiresAt,
                RemainingSeconds = (int)(expiresAt - heldAt).TotalSeconds
            };

            // Store in cache with TTL
            var cacheKey = CacheKeys.Format(CacheKeys.HeldSlot,
                request.DoctorId, request.Date.ToString(DateFormat), (int)request.AppointmentTimeId, userId);

            await _cacheService.SetAsync(cacheKey, holdSlot, TimeSpan.FromMinutes(HOLD_DURATION_MINUTES));

            LogInfo("Successfully held slot for user {UserId}: {CacheKey}",
                null, userId, cacheKey);

            return new HoldSlotResponse
            {
                Success = true,
                Message = "Đã giữ chỗ thành công",
                HoldSlot = holdSlot,
                RemainingSeconds = holdSlot.RemainingSeconds
            };

        }, "HoldSlot");
    }

    public async Task<bool> ReleaseSlotAsync(ReleaseSlotRequest request, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Releasing slot for doctor {DoctorId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null, request.DoctorId, request.Date, request.AppointmentTimeId, userId);

            var cacheKey = CacheKeys.Format(CacheKeys.HeldSlot,
                request.DoctorId, request.Date.ToString(DateFormat), (int)request.AppointmentTimeId, userId);

            await _cacheService.RemoveAsync(cacheKey);

            LogInfo("Successfully released slot: {CacheKey}", null, cacheKey);
            return true;

        }, "ReleaseSlot");
    }

    public async Task<List<AppointmentTime>> GetHeldSlotsAsync(Guid doctorId, DateOnly date, Guid currentUserId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogDebug("Getting held slots for doctor {DoctorId} on {Date} excluding user {CurrentUserId}",
                null, doctorId, date, currentUserId);

            var heldSlots = new List<AppointmentTime>();

            // Get all held slots for this doctor and date
            var pattern = CacheKeys.Format(CacheKeys.HeldSlotsByDoctorDate, doctorId, date.ToString(DateFormat));

            // Note: This is a simplified implementation. In a real Redis implementation,
            // you would use SCAN with pattern matching to get all matching keys.
            // For now, we'll iterate through all defined appointment time values.
            var appointmentTimes = Enum.GetValues<AppointmentTime>();

            foreach (var appointmentTime in appointmentTimes)
            {
                // Check if this slot is held by any user other than current user
                var isHeldByOther = await IsSlotHeldByOtherUserAsync(doctorId, date, appointmentTime, currentUserId);
                if (isHeldByOther)
                {
                    heldSlots.Add(appointmentTime);
                }
            }

            LogDebug("Found {Count} held slots for doctor {DoctorId} on {Date}",
                null, heldSlots.Count, doctorId, date);

            return heldSlots;

        }, "GetHeldSlots");
    }

    public async Task<bool> IsSlotHeldByOtherUserAsync(Guid doctorId, DateOnly date, AppointmentTime appointmentTimeId, Guid currentUserId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Pattern to find ALL users holding this specific slot
            var pattern = CacheKeys.Format(CacheKeys.HeldSlotByDoctorDateTimePattern,
                doctorId, date.ToString(DateFormat), (int)appointmentTimeId);

            LogDebug("Checking if slot is held by other users with pattern: {Pattern}", null, pattern);

            // Get all keys matching this pattern (all users holding this slot)
            var allKeysForThisSlot = await _cacheService.GetKeysByPatternAsync(pattern);

            // Check each key to see if it belongs to a different user
            foreach (var key in allKeysForThisSlot)
            {
                // Extract userId from key format: prefix:held_slot:{doctorId}:{date}:{timeId}:{userId}
                // Note: Key might have cache prefix, so we need to handle that
                var keyParts = key.Split(':');

                // Find the userId part (last segment after splitting by ':')
                if (keyParts.Length > 0)
                {
                    var userIdStr = keyParts[^1]; // Get last part (userId)

                    if (Guid.TryParse(userIdStr, out var holdingUserId))
                    {
                        if (holdingUserId != currentUserId)
                        {
                            // Slot is held by a DIFFERENT user
                            LogDebug("Slot {AppointmentTimeId} is held by user {HoldingUserId}, not current user {CurrentUserId}",
                                null, appointmentTimeId, holdingUserId, currentUserId);
                            return true;
                        }
                    }
                }
            }

            // No other user holds this slot
            LogDebug("Slot {AppointmentTimeId} is not held by any other user", null, appointmentTimeId);
            return false;

        }, "IsSlotHeldByOtherUser");
    }

    public async Task<int> ReleaseAllUserSlotsAsync(Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Releasing all held slots for user {UserId}", null, userId);

            var releasedCount = 0;

            // Use pattern to find all slots held by this user
            var pattern = CacheKeys.Format(CacheKeys.HeldSlotsByUser, userId);

            // Note: This is a simplified implementation
            // In a real Redis implementation, you would use SCAN with pattern matching
            await _cacheService.RemoveByPatternAsync(pattern);

            // For now, we'll assume 1 slot was released (since users can only hold 1 slot at a time)
            releasedCount = 1;

            LogInfo("Released {Count} slots for user {UserId}", null, releasedCount, userId);
            return releasedCount;

        }, "ReleaseAllUserSlots");
    }

    public async Task<int> GetRemainingTimeAsync(Guid doctorId, DateOnly date, AppointmentTime appointmentTimeId, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var cacheKey = CacheKeys.Format(CacheKeys.HeldSlot,
                doctorId, date.ToString(DateFormat), (int)appointmentTimeId, userId);

            var holdSlot = await _cacheService.GetAsync<HoldSlotDto>(cacheKey);
            if (holdSlot == null)
            {
                return 0;
            }

            var remainingSeconds = (int)(holdSlot.ExpiresAt - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, remainingSeconds);

        }, "GetRemainingTime");
    }
}
