using AutoMapper;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Notification.Models.DTOs;
using BookingCare.Services.Notification.Models.Entities;
using BookingCare.Services.Notification.Repositories.Interfaces;
using BookingCare.Services.Notification.Services.Interfaces;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Common.Services;

namespace BookingCare.Services.Notification.Services.Implementations;

public class NotificationService : BaseService, INotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;

    private const string UnreadCountCacheKeyPrefix = "notification:unread:";
    private const int CacheExpirationMinutes = 15;

    public NotificationService(
        INotificationRepository notificationRepository,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<NotificationService> logger) : base(logger)
    {
        _notificationRepository = notificationRepository;
        _cacheService = cacheService;
        _mapper = mapper;
    }

    public async Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Map DTO to Entity using AutoMapper
            var notification = _mapper.Map<NotificationEntity>(dto);

            // Save to MongoDB
            var created = await _notificationRepository.CreateAsync(notification, cancellationToken);

            // Invalidate unread count cache
            await InvalidateUnreadCountCacheAsync(dto.UserId);

            LogInfo("Created notification for user {UserId}, Type: {Type}, NotificationId: {NotificationId}",
                null, dto.UserId, dto.Type, created.Id);

            // Map Entity to DTO using AutoMapper
            return _mapper.Map<NotificationDto>(created);
        }, nameof(CreateNotificationAsync));
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(
        string userId,
        int pageNumber,
        int pageSize,
        bool? isRead = null,
        NotificationType? notificationType = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var skip = (pageNumber - 1) * pageSize;
            var notifications = await _notificationRepository.GetUserNotificationsAsync(
                userId, skip, pageSize, isRead, notificationType, cancellationToken);

            return _mapper.Map<List<NotificationDto>>(notifications);
        }, nameof(GetUserNotificationsAsync));
    }

    public async Task<bool> MarkAsReadAsync(string userId, string notificationId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Verify notification belongs to user
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null || notification.UserId != userId)
            {
                LogWarning("Notification {NotificationId} not found or doesn't belong to user {UserId}", null, notificationId, userId);
                return false;
            }

            if (notification.IsRead)
            {
                return true; // Already read
            }

            var result = await _notificationRepository.MarkAsReadAsync(notificationId, cancellationToken);

            if (result)
            {
                // Invalidate unread count cache
                await InvalidateUnreadCountCacheAsync(userId);
                LogInfo("Marked notification {NotificationId} as read for user {UserId}", null, notificationId, userId);
            }

            return result;
        }, nameof(MarkAsReadAsync));
    }

    public async Task<long> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var count = await _notificationRepository.MarkAllAsReadAsync(userId, cancellationToken);

            if (count > 0)
            {
                // Invalidate unread count cache
                await InvalidateUnreadCountCacheAsync(userId);
                LogInfo("Marked {Count} notifications as read for user {UserId}", null, count, userId);
            }

            return count;
        }, nameof(MarkAllAsReadAsync));
    }

    public async Task<bool> DeleteNotificationAsync(string userId, string notificationId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Verify notification belongs to user
            var notification = await _notificationRepository.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null || notification.UserId != userId)
            {
                LogWarning("Notification {NotificationId} not found or doesn't belong to user {UserId}", null, notificationId, userId);
                return false;
            }

            var result = await _notificationRepository.DeleteAsync(notificationId, cancellationToken);

            if (result)
            {
                // Invalidate unread count cache if it was unread
                if (!notification.IsRead)
                {
                    await InvalidateUnreadCountCacheAsync(userId);
                }
                LogInfo("Deleted notification {NotificationId} for user {UserId}", null, notificationId, userId);
            }

            return result;
        }, nameof(DeleteNotificationAsync));
    }

    public async Task<long> DeleteAllNotificationsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            var count = await _notificationRepository.DeleteAllAsync(userId, cancellationToken);

            if (count > 0)
            {
                // Invalidate unread count cache
                await InvalidateUnreadCountCacheAsync(userId);
                LogInfo("Deleted {Count} notifications for user {UserId}", null, count, userId);
            }

            return count;
        }, nameof(DeleteAllNotificationsAsync));
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            // Try to get unread count from cache
            var cacheKey = GetUnreadCountCacheKey(userId);
            var cachedCountStr = await _cacheService.GetAsync<string>(cacheKey);

            long unreadCount;
            if (string.IsNullOrEmpty(cachedCountStr))
            {
                // Cache miss - get from DB and cache it
                unreadCount = await _notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
                await _cacheService.SetAsync(cacheKey, unreadCount.ToString(), TimeSpan.FromMinutes(CacheExpirationMinutes));
            }
            else
            {
                unreadCount = long.Parse(cachedCountStr);
            }

            var totalCount = await _notificationRepository.GetTotalCountAsync(userId, cancellationToken);

            return new NotificationSummaryDto
            {
                TotalCount = totalCount,
                UnreadCount = unreadCount
            };
        }, nameof(GetSummaryAsync));
    }

    public async Task<Dictionary<NotificationType, long>> GetCountsByTypeAsync(string userId, bool? isRead = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithErrorHandling(async () =>
        {
            return await _notificationRepository.GetCountsByTypeAsync(userId, isRead, cancellationToken);
        }, nameof(GetCountsByTypeAsync));
    }

    private string GetUnreadCountCacheKey(string userId) => $"{UnreadCountCacheKeyPrefix}{userId}";

    private async Task InvalidateUnreadCountCacheAsync(string userId)
    {
        var cacheKey = GetUnreadCountCacheKey(userId);
        await _cacheService.RemoveAsync(cacheKey);
    }
}

