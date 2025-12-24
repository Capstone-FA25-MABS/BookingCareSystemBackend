using System.ComponentModel.DataAnnotations;

namespace BookingCare.Services.Favorites.Models.DTOs;

/// <summary>
/// Request DTO for creating a favorite
/// </summary>
public class CreateFavoriteRequest
{
    /// <summary>
    /// Patient ID
    /// </summary>
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID
    /// </summary>
    [Required]
    public Guid DoctorId { get; set; }
}

/// <summary>
/// Request DTO for toggling a favorite
/// </summary>
public class ToggleFavoriteRequest
{
    /// <summary>
    /// Patient ID
    /// </summary>
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID
    /// </summary>
    [Required]
    public Guid DoctorId { get; set; }
}

/// <summary>
/// Request DTO for checking multiple favorites
/// </summary>
public class CheckMultipleFavoritesRequest
{
    /// <summary>
    /// Patient ID
    /// </summary>
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>
    /// List of Doctor IDs to check
    /// </summary>
    [Required]
    public List<Guid> DoctorIds { get; set; } = new();
}

/// <summary>
/// Response DTO for favorite operations
/// </summary>
public class FavoriteResponse
{
    /// <summary>
    /// Favorite ID as Guid
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Patient ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID
    /// </summary>
    public Guid DoctorId { get; set; }

    /// <summary>
    /// Created timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response DTO for toggle favorite operation
/// </summary>
public class ToggleFavoriteResponse
{
    /// <summary>
    /// Patient ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID
    /// </summary>
    public Guid DoctorId { get; set; }

    /// <summary>
    /// Current favorite status after toggle
    /// </summary>
    public bool IsFavorited { get; set; }

    /// <summary>
    /// Action performed (Added or Removed)
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Favorite details (if added)
    /// </summary>
    public FavoriteResponse? Favorite { get; set; }

    /// <summary>
    /// Timestamp of the operation
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response DTO for checking multiple favorites
/// </summary>
public class CheckMultipleFavoritesResponse
{
    /// <summary>
    /// Patient ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// List of Doctor IDs that are favorited by the patient
    /// </summary>
    public List<Guid> FavoritedDoctorIds { get; set; } = new();

    /// <summary>
    /// Total number of doctors checked
    /// </summary>
    public int TotalChecked { get; set; }

    /// <summary>
    /// Total number of favorited doctors
    /// </summary>
    public int TotalFavorited { get; set; }

    /// <summary>
    /// Timestamp of the check
    /// </summary>
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request DTO for getting patient's favorites
/// </summary>
public class GetPatientFavoritesRequest
{
    /// <summary>
    /// Patient ID
    /// </summary>
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Page number for pagination
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size for pagination
    /// </summary>
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Request DTO for removing a favorite
/// </summary>
public class RemoveFavoriteRequest
{
    /// <summary>
    /// Patient ID
    /// </summary>
    [Required]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Doctor ID
    /// </summary>
    [Required]
    public Guid DoctorId { get; set; }
}