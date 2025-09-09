using BookingCare.Services.Favorites.Models.DTOs;
using BookingCare.Services.Favorites.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Favorites.Controllers;

/// <summary>
/// Controller for managing user favorites
/// </summary>
[ApiController]

[Route("api/[controller]")]
public class FavoritesController : BaseApiController
{
    private readonly IFavoriteService _favoriteService;
    private readonly IValidator<ToggleFavoriteRequest> _toggleValidator;
    private readonly IValidator<CheckMultipleFavoritesRequest> _checkMultipleValidator;
    private readonly IValidator<GetPatientFavoritesRequest> _getPatientFavoritesValidator;

    public FavoritesController(
        IFavoriteService favoriteService,
        IValidator<ToggleFavoriteRequest> toggleValidator,
        IValidator<CheckMultipleFavoritesRequest> checkMultipleValidator,
        IValidator<GetPatientFavoritesRequest> getPatientFavoritesValidator)
    {
        _favoriteService = favoriteService;
        _toggleValidator = toggleValidator;
        _checkMultipleValidator = checkMultipleValidator;
        _getPatientFavoritesValidator = getPatientFavoritesValidator;
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Success(new { Status = "Healthy", Service = "Favorites", Timestamp = DateTime.UtcNow }, "Service is healthy");
    }

    /// <summary>
    /// Get favorite by ID
    /// </summary>
    /// <param name="id">Favorite ID</param>
    /// <returns>Favorite details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetFavoriteById(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest("Invalid favorite ID");
        }

        var result = await _favoriteService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound($"Favorite with ID {id} not found");
        }
        return Success(result, "Favorite retrieved successfully");
    }

    /// <summary>
    /// Toggle favorite status - if exists remove it, if not exists add it
    /// </summary>
    /// <param name="request">Toggle favorite request</param>
    /// <returns>Toggle result with current status and action performed</returns>
    [HttpPost("toggle")]
    public async Task<IActionResult> ToggleFavorite([FromBody] ToggleFavoriteRequest request)
    {
        // Validate request
        var validationResult = await _toggleValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = errors,
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var result = await _favoriteService.ToggleFavoriteAsync(request);

        var message = result.Action == "Added"
            ? "Doctor added to favorites successfully"
            : "Doctor removed from favorites successfully";

        return Success(result, message);
    }

    /// <summary>
    /// Toggle favorite status using route parameters (alternative endpoint)
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Toggle result with current status and action performed</returns>
    [HttpPost("toggle/{patientId}/{doctorId}")]
    public async Task<IActionResult> ToggleFavoriteByIds(Guid patientId, Guid doctorId)
    {
        var request = new ToggleFavoriteRequest
        {
            PatientId = patientId,
            DoctorId = doctorId
        };

        // Validate request
        var validationResult = await _toggleValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = errors,
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var result = await _favoriteService.ToggleFavoriteAsync(request);

        var message = result.Action == "Added"
            ? "Doctor added to favorites successfully"
            : "Doctor removed from favorites successfully";

        return Success(result, message);
    }

    /// <summary>
    /// Check multiple doctors favorite status for a patient
    /// </summary>
    /// <param name="request">Check multiple favorites request</param>
    /// <returns>List of favorited doctor IDs with statistics</returns>
    [HttpPost("check-multiple")]
    public async Task<IActionResult> CheckMultipleFavorites([FromBody] CheckMultipleFavoritesRequest request)
    {
        // Validate request
        var validationResult = await _checkMultipleValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = errors,
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var result = await _favoriteService.CheckMultipleFavoritesAsync(request);
        return Success(result, $"Checked {result.TotalChecked} doctors, found {result.TotalFavorited} favorites");
    }

    /// <summary>
    /// Get patient's favorite doctors
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated list of favorites</returns>
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetPatientFavorites(
        Guid patientId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var request = new GetPatientFavoritesRequest
        {
            PatientId = patientId,
            Page = page,
            PageSize = pageSize
        };

        // Validate request
        var validationResult = await _getPatientFavoritesValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = errors,
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var result = await _favoriteService.GetPatientFavoritesAsync(request);
        return Paginated(result, "Patient favorites retrieved successfully");
    }

    /// <summary>
    /// Check if a doctor is favorited by a patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>True if favorited, false otherwise</returns>
    [HttpGet("check/{patientId}/{doctorId}")]
    public async Task<IActionResult> IsFavorited(Guid patientId, Guid doctorId)
    {
        if (patientId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Patient ID cannot be empty" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        if (doctorId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Doctor ID cannot be empty" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        if (patientId == doctorId)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Patient ID and Doctor ID cannot be the same" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var result = await _favoriteService.IsFavoritedAsync(patientId, doctorId);
        return Success(new { IsFavorited = result }, "Favorite status checked successfully");
    }

    /// <summary>
    /// Get favorite count for a doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Number of favorites</returns>
    [HttpGet("doctor/{doctorId}/count")]
    public async Task<IActionResult> GetDoctorFavoriteCount(Guid doctorId)
    {
        if (doctorId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Doctor ID cannot be empty" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var count = await _favoriteService.GetDoctorFavoriteCountAsync(doctorId);
        return Success(new { DoctorId = doctorId, FavoriteCount = count }, "Doctor favorite count retrieved successfully");
    }

    /// <summary>
    /// Get recent favorites for a patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="limit">Number of recent favorites to return (default: 5, max: 50)</param>
    /// <returns>List of recent favorites</returns>
    [HttpGet("patient/{patientId}/recent")]
    public async Task<IActionResult> GetRecentFavorites(Guid patientId, [FromQuery] int limit = 5)
    {
        if (patientId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Patient ID cannot be empty" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        if (limit < 1 || limit > 50)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Limit must be between 1 and 50" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var result = await _favoriteService.GetRecentFavoritesAsync(patientId, limit);
        return Success(result, "Recent favorites retrieved successfully");
    }

    /// <summary>
    /// Get doctor's favorites for analytics (admin endpoint)
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20)</param>
    /// <returns>Paginated list of doctor's favorites</returns>
    [HttpGet("doctor/{doctorId}/analytics")]
    public async Task<IActionResult> GetDoctorFavorites(
        Guid doctorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (doctorId == Guid.Empty)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Doctor ID cannot be empty" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        if (page < 1)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Page number must be greater than 0" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                success = false,
                message = "Validation failed",
                errors = new[] { "Page size must be between 1 and 100" },
                data = (object?)null,
                timestamp = DateTime.UtcNow
            });
        }

        var result = await _favoriteService.GetDoctorFavoritesAsync(doctorId, page, pageSize);
        return Paginated(result, "Doctor favorites analytics retrieved successfully");
    }
}