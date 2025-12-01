using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Enums;
using BookingCare.Shared.Common.Services;
using BookingCare.Services.Schedule.Enums;

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

    /// <summary>
    /// Get cache key prefix based on target type
    /// </summary>
    private static string GetTargetTypePrefix(HoldSlotTargetType targetType) =>
        targetType == HoldSlotTargetType.ServiceMedical ? "service" : "doctor";

    public async Task<HoldSlotResponse> HoldSlotAsync(HoldSlotRequest request, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var targetTypePrefix = GetTargetTypePrefix(request.TargetType);
            LogInfo("Attempting to hold slot for {TargetType} {TargetId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null, targetTypePrefix, request.TargetId, request.Date, request.AppointmentTimeId, userId);

            // Check if slot is already held by another user
            var isHeldByOther = await IsSlotHeldByOtherUserAsync(
                request.TargetId, request.TargetType, request.Date, request.AppointmentTimeId, userId);

            if (isHeldByOther)
            {
                LogWarning("Slot already held by another user: {TargetType}:{TargetId}:{Date}:{AppointmentTimeId}",
                    null, targetTypePrefix, request.TargetId, request.Date, request.AppointmentTimeId);

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
                TargetId = request.TargetId,
                TargetType = request.TargetType,
                Date = request.Date,
                AppointmentTimeId = request.AppointmentTimeId,
                UserId = userId,
                HeldAt = heldAt,
                ExpiresAt = expiresAt,
                RemainingSeconds = (int)(expiresAt - heldAt).TotalSeconds
            };

            // Store in cache with TTL - include target type in cache key
            var cacheKey = CacheKeys.Format(CacheKeys.HeldSlot,
                $"{targetTypePrefix}_{request.TargetId}", request.Date.ToString(DateFormat), (int)request.AppointmentTimeId, userId);

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
            var targetTypePrefix = GetTargetTypePrefix(request.TargetType);
            LogInfo("Releasing slot for {TargetType} {TargetId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null, targetTypePrefix, request.TargetId, request.Date, request.AppointmentTimeId, userId);

            var cacheKey = CacheKeys.Format(CacheKeys.HeldSlot,
                $"{targetTypePrefix}_{request.TargetId}", request.Date.ToString(DateFormat), (int)request.AppointmentTimeId, userId);

            await _cacheService.RemoveAsync(cacheKey);

            LogInfo("Successfully released slot: {CacheKey}", null, cacheKey);
            return true;

        }, "ReleaseSlot");
    }

    // New method with TargetType support
    public async Task<List<AppointmentTime>> GetHeldSlotsAsync(Guid targetId, HoldSlotTargetType targetType, DateOnly date, Guid currentUserId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var targetTypePrefix = GetTargetTypePrefix(targetType);
            LogDebug("Getting held slots for {TargetType} {TargetId} on {Date} excluding user {CurrentUserId}",
                null, targetTypePrefix, targetId, date, currentUserId);

            var heldSlots = new List<AppointmentTime>();
            var appointmentTimes = Enum.GetValues<AppointmentTime>();

            foreach (var appointmentTime in appointmentTimes)
            {
                var isHeldByOther = await IsSlotHeldByOtherUserAsync(targetId, targetType, date, appointmentTime, currentUserId);
                if (isHeldByOther)
                {
                    heldSlots.Add(appointmentTime);
                }
            }

            LogDebug("Found {Count} held slots for {TargetType} {TargetId} on {Date}",
                null, heldSlots.Count, targetTypePrefix, targetId, date);

            return heldSlots;

        }, "GetHeldSlots");
    }

    // Backward compatible method for Doctor
    public Task<List<AppointmentTime>> GetHeldSlotsAsync(Guid doctorId, DateOnly date, Guid currentUserId)
    {
        return GetHeldSlotsAsync(doctorId, HoldSlotTargetType.Doctor, date, currentUserId);
    }

    // New method with TargetType support
    public async Task<bool> IsSlotHeldByOtherUserAsync(Guid targetId, HoldSlotTargetType targetType, DateOnly date, AppointmentTime appointmentTimeId, Guid currentUserId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var targetTypePrefix = GetTargetTypePrefix(targetType);
            // Pattern to find ALL users holding this specific slot
            var pattern = CacheKeys.Format(CacheKeys.HeldSlotByDoctorDateTimePattern,
                $"{targetTypePrefix}_{targetId}", date.ToString(DateFormat), (int)appointmentTimeId);

            LogDebug("Checking if slot is held by other users with pattern: {Pattern}", null, pattern);

            // Get all keys matching this pattern (all users holding this slot)
            var allKeysForThisSlot = await _cacheService.GetKeysByPatternAsync(pattern);

            // Check each key to see if it belongs to a different user
            foreach (var key in allKeysForThisSlot)
            {
                var keyParts = key.Split(':');

                if (keyParts.Length > 0)
                {
                    var userIdStr = keyParts[^1];

                    if (Guid.TryParse(userIdStr, out var holdingUserId) && holdingUserId != currentUserId)
                    {
                        LogDebug("Slot {AppointmentTimeId} is held by user {HoldingUserId}, not current user {CurrentUserId}",
                            null, appointmentTimeId, holdingUserId, currentUserId);
                        return true;
                    }
                }
            }

            LogDebug("Slot {AppointmentTimeId} is not held by any other user", null, appointmentTimeId);
            return false;

        }, "IsSlotHeldByOtherUser");
    }

    // Backward compatible method for Doctor
    public Task<bool> IsSlotHeldByOtherUserAsync(Guid doctorId, DateOnly date, AppointmentTime appointmentTimeId, Guid currentUserId)
    {
        return IsSlotHeldByOtherUserAsync(doctorId, HoldSlotTargetType.Doctor, date, appointmentTimeId, currentUserId);
    }

    public async Task<int> ReleaseAllUserSlotsAsync(Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Releasing all held slots for user {UserId}", null, userId);

            var releasedCount = 0;

            // Use pattern to find all slots held by this user
            var pattern = CacheKeys.Format(CacheKeys.HeldSlotsByUser, userId);

            await _cacheService.RemoveByPatternAsync(pattern);

            releasedCount = 1;

            LogInfo("Released {Count} slots for user {UserId}", null, releasedCount, userId);
            return releasedCount;

        }, "ReleaseAllUserSlots");
    }

    // New method with TargetType support
    public async Task<int> GetRemainingTimeAsync(Guid targetId, HoldSlotTargetType targetType, DateOnly date, AppointmentTime appointmentTimeId, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var targetTypePrefix = GetTargetTypePrefix(targetType);
            var cacheKey = CacheKeys.Format(CacheKeys.HeldSlot,
                $"{targetTypePrefix}_{targetId}", date.ToString(DateFormat), (int)appointmentTimeId, userId);

            var holdSlot = await _cacheService.GetAsync<HoldSlotDto>(cacheKey);
            if (holdSlot == null)
            {
                return 0;
            }

            var remainingSeconds = (int)(holdSlot.ExpiresAt - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, remainingSeconds);

        }, "GetRemainingTime");
    }

    // Backward compatible method for Doctor
    public Task<int> GetRemainingTimeAsync(Guid doctorId, DateOnly date, AppointmentTime appointmentTimeId, Guid userId)
    {
        return GetRemainingTimeAsync(doctorId, HoldSlotTargetType.Doctor, date, appointmentTimeId, userId);
    }

    #region Specialty Hold Slot operations (for "hospital assigns doctor" mode)

    /// <summary>
    /// Get cache key for specialty hold slot using CacheKeys constant
    /// </summary>
    private static string GetSpecialtyHoldSlotCacheKey(Guid hospitalId, Guid specialtyId, DateOnly date, AppointmentTime appointmentTimeId, Guid userId)
    {
        return CacheKeys.Format(CacheKeys.SpecialtyHeldSlot, hospitalId, specialtyId, date.ToString(DateFormat), (int)appointmentTimeId, userId);
    }

    /// <summary>
    /// Get cache key pattern for specialty hold slots (for counting) using CacheKeys constant
    /// </summary>
    private static string GetSpecialtyHoldSlotPattern(Guid hospitalId, Guid specialtyId, DateOnly date, AppointmentTime appointmentTimeId)
    {
        return CacheKeys.Format(CacheKeys.SpecialtyHeldSlotPattern, hospitalId, specialtyId, date.ToString(DateFormat), (int)appointmentTimeId);
    }

    public async Task<HoldSlotResponse> HoldSpecialtySlotAsync(HoldSpecialtySlotRequest request, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            LogInfo("Attempting to hold specialty slot for hospital {HospitalId}, specialty {SpecialtyId} on {Date} at {AppointmentTimeId} by user {UserId}",
                null, request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, userId);

            // Check current held count for this slot
            var currentHeldCount = await GetSpecialtyHeldCountAsync(
                request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, Guid.Empty);

            // Check if capacity is exceeded
            if (currentHeldCount >= request.MaxCapacity)
            {
                LogWarning("Specialty slot capacity exceeded: {HospitalId}:{SpecialtyId}:{Date}:{AppointmentTimeId} - {CurrentCount}/{MaxCapacity}",
                    null, request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, currentHeldCount, request.MaxCapacity);

                return new HoldSlotResponse
                {
                    Success = false,
                    Message = "Khung giờ này đã hết chỗ. Vui lòng chọn khung giờ khác.",
                    RemainingSeconds = 0
                };
            }

            // Check if this user already holds this slot
            var existingHoldKey = GetSpecialtyHoldSlotCacheKey(
                request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, userId);
            var existingHold = await _cacheService.GetAsync<HoldSlotDto>(existingHoldKey);

            if (existingHold != null)
            {
                // User already holds this slot, return remaining time
                var remainingSeconds = (int)(existingHold.ExpiresAt - DateTime.UtcNow).TotalSeconds;
                if (remainingSeconds > 0)
                {
                    return new HoldSlotResponse
                    {
                        Success = true,
                        Message = "Bạn đã giữ chỗ này trước đó.",
                        HoldSlot = existingHold,
                        RemainingSeconds = remainingSeconds
                    };
                }
            }

            // Release any existing specialty slots held by this user (user can only hold one specialty slot at a time)
            await ReleaseAllUserSpecialtySlotsAsync(userId);

            // Create hold slot data
            var heldAt = DateTime.UtcNow;
            var expiresAt = heldAt.AddMinutes(HOLD_DURATION_MINUTES);
            var holdSlot = new HoldSlotDto
            {
                TargetId = request.SpecialtyId, // Use specialty ID as target
                TargetType = HoldSlotTargetType.Specialty,
                Date = request.Date,
                AppointmentTimeId = request.AppointmentTimeId,
                UserId = userId,
                HeldAt = heldAt,
                ExpiresAt = expiresAt,
                RemainingSeconds = (int)(expiresAt - heldAt).TotalSeconds
            };

            // Store in cache with TTL
            var cacheKey = GetSpecialtyHoldSlotCacheKey(
                request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, userId);

            await _cacheService.SetAsync(cacheKey, holdSlot, TimeSpan.FromMinutes(HOLD_DURATION_MINUTES));

            LogInfo("Successfully held specialty slot: {HospitalId}:{SpecialtyId}:{Date}:{AppointmentTimeId} for user {UserId}",
                null, request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, userId);

            return new HoldSlotResponse
            {
                Success = true,
                Message = "Đã giữ chỗ thành công. Vui lòng hoàn tất đặt lịch trong 5 phút.",
                HoldSlot = holdSlot,
                RemainingSeconds = holdSlot.RemainingSeconds
            };

        }, "HoldSpecialtySlot");
    }

    public async Task<bool> ReleaseSpecialtySlotAsync(ReleaseSpecialtySlotRequest request, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var cacheKey = GetSpecialtyHoldSlotCacheKey(
                request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, userId);

            await _cacheService.RemoveAsync(cacheKey);

            LogInfo("Released specialty slot: {HospitalId}:{SpecialtyId}:{Date}:{AppointmentTimeId} for user {UserId}",
                null, request.HospitalId, request.SpecialtyId, request.Date, request.AppointmentTimeId, userId);

            return true;

        }, "ReleaseSpecialtySlot");
    }

    public async Task<int> GetSpecialtyHeldCountAsync(Guid hospitalId, Guid specialtyId, DateOnly date, AppointmentTime appointmentTimeId, Guid currentUserId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var pattern = GetSpecialtyHoldSlotPattern(hospitalId, specialtyId, date, appointmentTimeId);

            // Get all keys matching the pattern
            var keys = await _cacheService.GetKeysByPatternAsync(pattern);

            // Count valid (non-expired) holds, excluding current user
            var count = 0;
            foreach (var key in keys)
            {
                // Extract userId from key - it's the last part
                // Key format: [prefix:]holdslot:specialty:{hospitalId}:{specialtyId}:{date}:{appointmentTimeId}:{userId}
                var parts = key.Split(':');
                var userIdPart = parts[^1]; // Last element

                if (Guid.TryParse(userIdPart, out var holdUserId) && holdUserId != currentUserId)
                {
                    // Use GetByFullKeyAsync since key from GetKeysByPatternAsync already includes prefix
                    var holdSlot = await _cacheService.GetByFullKeyAsync<HoldSlotDto>(key);
                    if (holdSlot != null && holdSlot.ExpiresAt > DateTime.UtcNow)
                    {
                        count++;
                    }
                }
            }

            return count;

        }, "GetSpecialtyHeldCount");
    }

    public async Task<int> GetSpecialtyRemainingTimeAsync(Guid hospitalId, Guid specialtyId, DateOnly date, AppointmentTime appointmentTimeId, Guid userId)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var cacheKey = GetSpecialtyHoldSlotCacheKey(hospitalId, specialtyId, date, appointmentTimeId, userId);

            var holdSlot = await _cacheService.GetAsync<HoldSlotDto>(cacheKey);
            if (holdSlot == null)
            {
                return 0;
            }

            var remainingSeconds = (int)(holdSlot.ExpiresAt - DateTime.UtcNow).TotalSeconds;
            return Math.Max(0, remainingSeconds);

        }, "GetSpecialtyRemainingTime");
    }

    /// <summary>
    /// Release all specialty slots held by a user
    /// </summary>
    private async Task ReleaseAllUserSpecialtySlotsAsync(Guid userId)
    {
        try
        {
            // Pattern to find all specialty slots held by this user
            var pattern = CacheKeys.Format(CacheKeys.SpecialtyHeldSlotsByUser, userId);
            await _cacheService.RemoveByPatternAsync(pattern);
            LogInfo("Released all specialty slots for user {UserId}", null, userId);
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to release all specialty slots for user {userId}: {ex.Message}");
        }
    }

    #endregion
}
