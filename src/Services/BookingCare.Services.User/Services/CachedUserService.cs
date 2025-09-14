using BookingCare.Services.User.Models;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Constants;

namespace BookingCare.Services.User.Services;

/// <summary>
/// User service with caching implementation
/// This is an example service demonstrating how to use the cache service
/// </summary>
public class CachedUserService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedUserService> _logger;

    public CachedUserService(ICacheService cacheService, ILogger<CachedUserService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get user by ID with caching
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User model or null if not found</returns>
    public async Task<UserModel?> GetUserByIdAsync(int userId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.UserById, userId);

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogInformation("Cache miss for user ID {UserId}. Fetching from database", userId);

            // Simulate database call
            var user = await FetchUserFromDatabaseAsync(userId);

            if (user != null)
            {
                _logger.LogInformation("User {UserId} fetched from database and cached", userId);
            }

            return user;
        }, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
    }

    /// <summary>
    /// Get user by email with caching
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User model or null if not found</returns>
    public async Task<UserModel?> GetUserByEmailAsync(string email)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.UserByEmail, email.ToLowerInvariant());

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogInformation("Cache miss for user email {Email}. Fetching from database", email);

            // Simulate database call
            var user = await FetchUserByEmailFromDatabaseAsync(email);

            if (user != null)
            {
                _logger.LogInformation("User with email {Email} fetched from database and cached", email);
            }

            return user;
        }, TimeSpan.FromMinutes(15)); // Cache for 15 minutes
    }

    /// <summary>
    /// Update user and invalidate cache
    /// </summary>
    /// <param name="user">User model to update</param>
    public async Task UpdateUserAsync(UserModel user)
    {
        // Simulate database update
        await UpdateUserInDatabaseAsync(user);

        // Invalidate cache entries for this user
        await InvalidateUserCacheAsync(user.Id, user.Email);

        _logger.LogInformation("User {UserId} updated and cache invalidated", user.Id);
    }

    /// <summary>
    /// Delete user and invalidate cache
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <param name="email">User email for cache invalidation</param>
    public async Task DeleteUserAsync(int userId, string email)
    {
        // Simulate database deletion
        await DeleteUserFromDatabaseAsync(userId);

        // Invalidate cache entries for this user
        await InvalidateUserCacheAsync(userId, email);

        _logger.LogInformation("User {UserId} deleted and cache invalidated", userId);
    }

    /// <summary>
    /// Get user profile with additional caching layers
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User profile data</returns>
    public async Task<object?> GetUserProfileAsync(int userId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.UserProfile, userId);

        return await _cacheService.GetOrSetAsync(cacheKey, async () =>
        {
            _logger.LogInformation("Building user profile for user {UserId}", userId);

            var user = await GetUserByIdAsync(userId);
            if (user == null) return null;

            // Simulate building complex profile data
            var profile = new
            {
                User = user,
                LastLoginDate = DateTime.UtcNow.AddDays(-1),
                AppoiAppointmentCountntmentCount = 5,
                ProfileCompleteness = 85.5m,
                PreferredLanguage = "en-US"
            };

            _logger.LogInformation("User profile built and cached for user {UserId}", userId);
            return profile;
        }, TimeSpan.FromHours(2)); // Cache for 2 hours
    }

    /// <summary>
    /// Bulk cache invalidation for user-related data
    /// </summary>
    /// <param name="userId">User ID</param>
    public async Task InvalidateAllUserDataAsync(int userId)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return;

        // Remove all cache entries related to this user
        await InvalidateUserCacheAsync(userId, user.Email);

        // Remove user profile cache
        var profileCacheKey = CacheKeys.Format(CacheKeys.UserProfile, userId);
        await _cacheService.RemoveAsync(profileCacheKey);

        _logger.LogInformation("All cache data invalidated for user {UserId}", userId);
    }

    #region Private Helper Methods

    private async Task<UserModel?> FetchUserFromDatabaseAsync(int userId)
    {
        // Simulate database delay
        await Task.Delay(100);

        // Simulate user data from database
        if (userId <= 0) return null;

        return new UserModel
        {
            Id = userId,
            Email = $"user{userId}@bookingcare.com",
            FirstName = $"User{userId}",
            LastName = "Test",
            PhoneNumber = $"+1234567{userId:D3}",
            DateOfBirth = DateTime.UtcNow.AddYears(-25),
            Address = $"{userId} Test Street, Test City",
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };
    }

    private async Task<UserModel?> FetchUserByEmailFromDatabaseAsync(string email)
    {
        // Simulate database delay
        await Task.Delay(100);

        // Extract user ID from email for simulation
        if (email.StartsWith("user") && email.Contains("@"))
        {
            var userIdStr = email.Substring(4, email.IndexOf('@') - 4);
            if (int.TryParse(userIdStr, out var userId))
            {
                return await FetchUserFromDatabaseAsync(userId);
            }
        }

        return null;
    }

    private async Task UpdateUserInDatabaseAsync(UserModel user)
    {
        // Simulate database update
        await Task.Delay(50);
        user.UpdatedAt = DateTime.UtcNow;
    }

    private async Task DeleteUserFromDatabaseAsync(int userId)
    {
        // Simulate database deletion
        await Task.Delay(50);
    }

    private async Task InvalidateUserCacheAsync(int userId, string email)
    {
        var userByIdKey = CacheKeys.Format(CacheKeys.UserById, userId);
        var userByEmailKey = CacheKeys.Format(CacheKeys.UserByEmail, email.ToLowerInvariant());

        await Task.WhenAll(
            _cacheService.RemoveAsync(userByIdKey),
            _cacheService.RemoveAsync(userByEmailKey)
        );
    }

    #endregion
}
