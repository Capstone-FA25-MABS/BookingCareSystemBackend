using BookingCare.Services.Favorites.Models.DTOs;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Favorites.Services.Interfaces;

/// <summary>
/// Service interface for Favorite business logic
/// </summary>
public interface IFavoriteService
{
    /// <summary>
    /// Get favorite by ID
    /// </summary>
    /// <param name="id">Favorite ID</param>
    /// <returns>Favorite response or null</returns>
    Task<FavoriteResponse?> GetByIdAsync(Guid id);

    /// <summary>
    /// Add a doctor to patient's favorites
    /// </summary>
    /// <param name="request">Create favorite request</param>
    /// <returns>Created favorite response</returns>
    Task<FavoriteResponse> AddFavoriteAsync(CreateFavoriteRequest request);

    /// <summary>
    /// Toggle favorite status - if exists remove it, if not exists add it
    /// </summary>
    /// <param name="request">Toggle favorite request</param>
    /// <returns>Toggle result with current status</returns>
    Task<ToggleFavoriteResponse> ToggleFavoriteAsync(ToggleFavoriteRequest request);

    /// <summary>
    /// Check multiple doctors favorite status for a patient
    /// </summary>
    /// <param name="request">Check multiple favorites request</param>
    /// <returns>List of favorited doctor IDs</returns>
    Task<CheckMultipleFavoritesResponse> CheckMultipleFavoritesAsync(CheckMultipleFavoritesRequest request);

    /// <summary>
    /// Remove a doctor from patient's favorites
    /// </summary>
    /// <param name="request">Remove favorite request</param>
    /// <returns>Success status</returns>
    Task<bool> RemoveFavoriteAsync(RemoveFavoriteRequest request);

    /// <summary>
    /// Get patient's favorite doctors with pagination
    /// </summary>
    /// <param name="request">Get patient favorites request</param>
    /// <returns>Paginated list of favorites</returns>
    Task<PagedResult<FavoriteResponse>> GetPatientFavoritesAsync(GetPatientFavoritesRequest request);

    /// <summary>
    /// Check if a doctor is favorited by a patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>True if favorited, false otherwise</returns>
    Task<bool> IsFavoritedAsync(Guid patientId, Guid doctorId);

    /// <summary>
    /// Get favorite count for a doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Number of favorites</returns>
    Task<long> GetDoctorFavoriteCountAsync(Guid doctorId);

    /// <summary>
    /// Get recent favorites for a patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="limit">Number of recent favorites to return</param>
    /// <returns>List of recent favorites</returns>
    Task<List<FavoriteResponse>> GetRecentFavoritesAsync(Guid patientId, int limit = 5);

    /// <summary>
    /// Get doctor's favorites for analytics (admin only)
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of doctor's favorites</returns>
    Task<PagedResult<FavoriteResponse>> GetDoctorFavoritesAsync(Guid doctorId, int page = 1, int pageSize = 20);
}