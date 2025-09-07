using BookingCare.Services.Favorites.Models.Entities;
using BookingCare.Shared.Common.Models;

namespace BookingCare.Services.Favorites.Repositories.Interfaces;

/// <summary>
/// Repository interface for Favorite operations
/// </summary>
public interface IFavoriteRepository
{
    /// <summary>
    /// Create a new favorite
    /// </summary>
    /// <param name="favorite">Favorite entity to create</param>
    /// <returns>Created favorite entity</returns>
    Task<FavoriteEntity> CreateAsync(FavoriteEntity favorite);

    /// <summary>
    /// Get favorite by ID
    /// </summary>
    /// <param name="id">Favorite ID as Guid</param>
    /// <returns>Favorite entity or null</returns>
    Task<FavoriteEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get favorite by patient and doctor IDs
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Favorite entity or null</returns>
    Task<FavoriteEntity?> GetByPatientAndDoctorAsync(Guid patientId, Guid doctorId);

    /// <summary>
    /// Check multiple doctors for favorites by a patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorIds">List of Doctor IDs</param>
    /// <returns>List of favorited doctor IDs</returns>
    Task<List<Guid>> CheckMultipleFavoritesAsync(Guid patientId, List<Guid> doctorIds);

    /// <summary>
    /// Get patient's favorites with pagination
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of favorites</returns>
    Task<PagedResult<FavoriteEntity>> GetPatientFavoritesAsync(Guid patientId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Get all favorites for a doctor (for analytics)
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of favorites</returns>
    Task<PagedResult<FavoriteEntity>> GetDoctorFavoritesAsync(Guid doctorId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Remove a favorite
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>True if removed, false if not found</returns>
    Task<bool> RemoveAsync(Guid patientId, Guid doctorId);

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
    Task<List<FavoriteEntity>> GetRecentFavoritesAsync(Guid patientId, int limit = 5);
}