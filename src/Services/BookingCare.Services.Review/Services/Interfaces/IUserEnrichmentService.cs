using BookingCare.Services.Review.Models.DTOs;

namespace BookingCare.Services.Review.Services.Interfaces;

/// <summary>
/// Interface for enriching user information from User service
/// </summary>
public interface IUserEnrichmentService
{
    /// <summary>
    /// Gets user information for multiple user IDs (for patients who write reviews)
    /// </summary>
    /// <param name="userIds">List of user IDs to fetch</param>
    /// <returns>Dictionary mapping user ID to UserInfo</returns>
    Task<Dictionary<string, UserInfo>> GetUsersInfoAsync(List<string> userIds);
}