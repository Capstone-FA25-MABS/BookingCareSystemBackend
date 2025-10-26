using BookingCare.Services.Doctor.Constants;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class DoctorsController : BaseApiController
{
    private readonly IDoctorService _doctorService;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly ILogger<DoctorsController> _logger;

    public DoctorsController(
        IDoctorService doctorService,
        FileUploadOrchestrator uploadOrchestrator,
        ILogger<DoctorsController> logger)
    {
        _doctorService = doctorService;
        _uploadOrchestrator = uploadOrchestrator;
        _logger = logger;
    }

    #region Health Check

    /// <summary>
    /// Health check endpoint - Available in all versions
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Doctor",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Doctor Endpoints

    /// <summary>
    /// Filter doctors nâng cao theo nhiều tiêu chí (chuyên khoa, lịch trống, gender, số năm kinh nghiệm, giá, phòng khám, loại tư vấn, ngôn ngữ, đánh giá, địa chỉ, loại hình dịch vụ) - Optimized response
    /// </summary>
    [HttpPost("filter")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> FilterDoctors([FromBody] DoctorAdvancedFilterRequest filter)
    {
        // Use optimized filter method that returns only necessary fields
        var result = await _doctorService.FilterDoctorsOptimizedAsync(filter);
        return Success<DoctorSearchListResponse>(result, "Doctors filtered successfully");
    }

    /// <summary>
    /// Get doctor by ID with optimized response (only essential fields)
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctor(Guid id)
    {
        var doctor = await _doctorService.GetDoctorByIdAsync(id);
        if (doctor == null)
        {
            return NotFound($"Doctor with ID {id} not found");
        }

        return Success<DoctorByIdResponse>(doctor, "Doctor retrieved successfully");
    }

    /// <summary>
    /// Get doctor by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorByEmail(string email)
    {
        var doctor = await _doctorService.GetDoctorByEmailAsync(email);
        if (doctor == null)
        {
            return NotFound($"Doctor with email '{email}' not found");
        }

        return Success<DoctorResponse>(doctor, "Doctor retrieved successfully");
    }

    /// <summary>
    /// Get doctor by account ID
    /// </summary>
    [HttpGet("by-account")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorByAccountId()
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            var doctor = await _doctorService.GetDoctorByAccountIdAsync(accountId);
            if (doctor == null)
            {
                return NotFound($"Doctor with account ID {accountId} not found");
            }
            return Success(doctor, "Doctor retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }

    }

    /// <summary>
    /// Get all doctors with filtering and pagination (Admin only - includes ACTIVE and INACTIVE)
    /// </summary>
    [HttpGet("admin")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorsForAdmin([FromQuery] DoctorQueryRequest query)
    {
        var result = await _doctorService.GetDoctorsAsync(query);
        return Success<DoctorListResponse>(result, "All doctors retrieved successfully for admin");
    }

    /// <summary>
    /// Get active doctors for patients with filtering and pagination
    /// </summary>
    [HttpGet("patients/active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveDoctorsForPatients([FromQuery] DoctorQueryRequest query, [FromQuery] Guid? patientId)
    {
        // Only return ACTIVE doctors for patients
        query.Status = BookingCare.Shared.Common.Enums.Status.ACTIVE;

        DoctorListResponse result;
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            result = await _doctorService.GetDoctorsWithFavoriteStatusAsync(query, patientId.Value);
        }
        else
        {
            result = await _doctorService.GetDoctorsAsync(query);
        }
        return Success<DoctorListResponse>(result, "Active doctors retrieved successfully for patients");
    }

    /// <summary>
    /// Get doctors with filtering and pagination (Legacy - for backward compatibility)
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctors([FromQuery] DoctorQueryRequest query, [FromQuery] Guid? patientId)
    {
        DoctorListResponse result;
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            result = await _doctorService.GetDoctorsWithFavoriteStatusAsync(query, patientId.Value);
        }
        else
        {
            result = await _doctorService.GetDoctorsAsync(query);
        }
        return Success<DoctorListResponse>(result, "Doctors retrieved successfully");
    }

    /// <summary>
    /// Get doctors by hospital with optimized response for hospital staff (includes both ACTIVE and INACTIVE doctors)
    /// </summary>
    [HttpGet("hospital/{hospitalId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorsByHospital(Guid hospitalId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _doctorService.GetDoctorsByHospitalOptimizedAsync(hospitalId, pageNumber, pageSize);
        return Success<DoctorSearchListResponse>(result, $"Doctors for hospital {hospitalId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by specialty
    /// </summary>
    [HttpGet("specialty/{specialtyId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorsBySpecialty(Guid specialtyId)
    {
        var doctors = await _doctorService.GetDoctorsBySpecialtyAsync(specialtyId);
        return Success<List<DoctorResponse>>(doctors, $"Doctors for specialty {specialtyId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by position
    /// </summary>
    [HttpGet("position/{positionId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorsByPosition(Guid positionId)
    {
        var doctors = await _doctorService.GetDoctorsByPositionAsync(positionId);
        return Success<List<DoctorResponse>>(doctors, $"Doctors for position {positionId} retrieved successfully");
    }

    /// <summary>
    /// Get active doctors (simple list)
    /// </summary>
    [HttpGet("list/active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveDoctors()
    {
        var doctors = await _doctorService.GetActiveDoctorsAsync();
        return Success<List<DoctorResponse>>(doctors, "Active doctors retrieved successfully");
    }

    /// <summary>
    /// Get patient's favorite doctors (Doctor info), default pageSize=9
    /// </summary>
    [HttpGet("patient/{patientId}/favorites")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPatientFavoriteDoctors(Guid patientId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 9, [FromQuery] string? searchTerm = null)
    {
        var result = await _doctorService.GetPatientFavoriteDoctorsAsync(patientId, pageNumber, pageSize, searchTerm);
        return Success<DoctorListResponse>(result, $"Favorite doctors for patient {patientId} retrieved successfully");
    }

    /// <summary>
    /// Search active doctors by name, specialty, or location for patients (Optimized response with only necessary fields)
    /// </summary>
    [HttpGet("patients/search")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> SearchActiveDoctors([FromQuery] string? searchTerm, [FromQuery] Guid? specialtyId, [FromQuery] Guid? hospitalId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? patientId = null)
    {
        var query = new DoctorQueryRequest
        {
            SearchTerm = searchTerm,
            SpecialtyId = specialtyId,
            HospitalId = hospitalId,
            Status = BookingCare.Shared.Common.Enums.Status.ACTIVE, // Only active doctors
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Use optimized search method that returns only necessary fields
        var result = await _doctorService.SearchDoctorsForPatientsAsync(query, patientId);
        return Success<DoctorSearchListResponse>(result, "Active doctors search completed successfully");
    }

    /// <summary>
    /// Get active doctors by specialty for patients
    /// </summary>
    [HttpGet("patients/specialty/{specialtyId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveDoctorsBySpecialty(Guid specialtyId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? patientId = null)
    {
        var query = new DoctorQueryRequest
        {
            SpecialtyId = specialtyId,
            Status = BookingCare.Shared.Common.Enums.Status.ACTIVE, // Only active doctors
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        DoctorListResponse result;
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            result = await _doctorService.GetDoctorsWithFavoriteStatusAsync(query, patientId.Value);
        }
        else
        {
            result = await _doctorService.GetDoctorsAsync(query);
        }
        return Success<DoctorListResponse>(result, $"Active doctors in specialty {specialtyId} retrieved successfully");
    }

    /// <summary>
    /// Get active doctors by hospital for patients
    /// </summary>
    [HttpGet("patients/hospital/{hospitalId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveDoctorsByHospital(Guid hospitalId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] Guid? patientId = null)
    {
        var query = new DoctorQueryRequest
        {
            HospitalId = hospitalId,
            Status = BookingCare.Shared.Common.Enums.Status.ACTIVE, // Only active doctors
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        DoctorListResponse result;
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            result = await _doctorService.GetDoctorsWithFavoriteStatusAsync(query, patientId.Value);
        }
        else
        {
            result = await _doctorService.GetDoctorsAsync(query);
        }
        return Success<DoctorListResponse>(result, $"Active doctors in hospital {hospitalId} retrieved successfully");
    }

    /// <summary>
    /// Get featured/recommended active doctors for patients
    /// </summary>
    [HttpGet("patients/featured")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetFeaturedActiveDoctors([FromQuery] int limit = 6, [FromQuery] Guid? patientId = null)
    {
        var query = new DoctorQueryRequest
        {
            Status = BookingCare.Shared.Common.Enums.Status.ACTIVE, // Only active doctors
            SortBy = "YearsOfExperience", // Sort by experience
            SortOrder = "desc", // Most experienced first
            PageNumber = 1,
            PageSize = limit
        };

        DoctorListResponse result;
        if (patientId.HasValue && patientId.Value != Guid.Empty)
        {
            result = await _doctorService.GetDoctorsWithFavoriteStatusAsync(query, patientId.Value);
        }
        else
        {
            result = await _doctorService.GetDoctorsAsync(query);
        }
        return Success<DoctorListResponse>(result, "Featured active doctors retrieved successfully");
    }


    /// <summary>
    /// Create a new doctor
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(DoctorConstants.ValidationMessages.InvalidRequestData, ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        try
        {
            var doctor = await _doctorService.CreateDoctorAsync(request);
            return Created(doctor, "Doctor created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor: {Message}", ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message,
                errors = new[] { ex.GetType().Name },
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Update doctor information
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateDoctor(Guid id, [FromBody] UpdateDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(DoctorConstants.ValidationMessages.InvalidRequestData, ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id; // Ensure the ID in the request matches the route parameter
        var doctor = await _doctorService.UpdateDoctorAsync(request);
        return Success<DoctorResponse>(doctor, "Doctor updated successfully");
    }

    /// <summary>
    /// Create a new doctor with avatar upload
    /// </summary>
    [HttpPost("upload-avatar")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> CreateDoctorWithAvatar(
        [FromForm] CreateDoctorRequest request,
        [FromForm] IFormFile? avatarFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(DoctorConstants.ValidationMessages.InvalidRequestData, ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            // Handle avatar upload if provided
            if (avatarFile != null)
            {
                var config = new FileUploadConfig
                {
                    AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
                    MaxSizeInMB = 5,
                    Folder = "avatars/doctors",
                    SuccessMessage = "Doctor avatar uploaded successfully",
                    EntityType = "doctor-avatar"
                };

                var uploadResult = await _uploadOrchestrator.UploadFileAsync(avatarFile, config, request.AccountId, _logger, cancellationToken);

                if (!uploadResult.Success)
                {
                    return BadRequest($"Avatar upload failed: {uploadResult.ErrorMessage}");
                }

                // Set the avatar URL from upload result - use CloudFront URL for public access
                request.AvatarUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
            }

            var doctor = await _doctorService.CreateDoctorAsync(request);
            return Created(doctor, "Doctor created successfully with avatar");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor with avatar: {Message}", ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message,
                errors = new[] { ex.GetType().Name },
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Update doctor information with avatar upload
    /// </summary>
    [HttpPut("{id}/upload-avatar")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize]
    public async Task<IActionResult> UpdateDoctorWithAvatar(
        Guid id,
        [FromForm] UpdateDoctorRequest request,
        [FromForm] IFormFile? avatarFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(DoctorConstants.ValidationMessages.InvalidRequestData, ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            request.Id = id; // Ensure the ID in the request matches the route parameter

            // Get current doctor to get account ID for avatar upload
            var currentDoctor = await _doctorService.GetDoctorByIdAsync(id);
            if (currentDoctor == null)
            {
                return NotFound($"Doctor with ID {id} not found");
            }

            // Handle avatar upload if provided
            if (avatarFile != null)
            {
                // Delete old avatar if exists (not default avatar)
                if (!string.IsNullOrEmpty(currentDoctor.AvatarUrl) &&
                    currentDoctor.AvatarUrl != "https://bookingcaree.com/user-avatar-default.png")
                {
                    var deleteConfig = new FileDeletionConfig
                    {
                        FileUrl = currentDoctor.AvatarUrl,
                        ExpectedFolder = "avatars",
                        SuccessMessage = "Old avatar deleted successfully",
                        EntityType = "doctor-avatar"
                    };

                    var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, currentDoctor.AccountId, _logger, cancellationToken);
                    if (!deleteResult.Success)
                    {
                        _logger.LogWarning("Failed to delete old avatar for doctor {DoctorId}: {Error}", id, deleteResult.ErrorMessage);
                        // Continue with upload even if deletion fails
                    }
                }

                var config = new FileUploadConfig
                {
                    AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
                    MaxSizeInMB = 5,
                    Folder = "avatars/doctors",
                    SuccessMessage = "Doctor avatar uploaded successfully",
                    EntityType = "doctor-avatar"
                };

                var uploadResult = await _uploadOrchestrator.UploadFileAsync(avatarFile, config, currentDoctor.AccountId, _logger, cancellationToken);

                if (!uploadResult.Success)
                {
                    return BadRequest($"Avatar upload failed: {uploadResult.ErrorMessage}");
                }

                // Set the avatar URL from upload result - use CloudFront URL for public access
                request.AvatarUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
            }

            var doctor = await _doctorService.UpdateDoctorAsync(request);
            return Success<DoctorResponse>(doctor, "Doctor updated successfully with avatar");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating doctor with avatar: {Message}", ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message,
                errors = new[] { ex.GetType().Name },
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Delete doctor
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteDoctor(Guid id)
    {
        var result = await _doctorService.DeleteDoctorAsync(id);
        if (!result)
        {
            return NotFound($"Doctor with ID {id} not found");
        }

        return Success<object?>(null, "Doctor deleted successfully");
    }

    /// <summary>
    /// Toggle doctor status (ACTIVE/INACTIVE)
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ToggleDoctorStatus(Guid id)
    {
        var result = await _doctorService.ToggleDoctorStatusAsync(id);
        if (!result)
        {
            return NotFound($"Doctor with ID {id} not found");
        }

        return Success<object?>(null, "Doctor status toggled successfully");
    }

    /// <summary>
    /// Validate doctor existence
    /// </summary>
    [HttpGet("{id}/validate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateDoctor(Guid id)
    {
        var exists = await _doctorService.DoctorExistsAsync(id);
        return Success<object>(new { exists }, "Doctor validation completed");
    }

    #endregion

    #region Doctor-Price Relationship Endpoints

    /// <summary>
    /// Get doctor's prices
    /// </summary>
    [HttpGet("{doctorId}/prices")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDoctorPrices(Guid doctorId)
    {
        var prices = await _doctorService.GetDoctorPricesAsync(doctorId);
        return Success<List<DoctorPriceResponse>>(prices, $"Prices for doctor {doctorId} retrieved successfully");
    }

    /// <summary>
    /// Assign price to doctor
    /// </summary>
    [HttpPost("assign-price")]
    public async Task<IActionResult> AssignPriceToDoctor([FromBody] AssignPriceToDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(DoctorConstants.ValidationMessages.InvalidRequestData, ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var doctorPrice = await _doctorService.AssignPriceToDoctorAsync(request);
        return Created(doctorPrice, "Price assigned to doctor successfully");
    }

    /// <summary>
    /// Remove price from doctor
    /// </summary>
    [HttpDelete("{doctorId}/prices/{doctorPriceId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> RemovePriceFromDoctor(Guid doctorId, Guid doctorPriceId)
    {
        var result = await _doctorService.RemovePriceFromDoctorAsync(doctorId, doctorPriceId);
        if (!result)
        {
            return NotFound($"Price {doctorPriceId} not assigned to doctor {doctorId}");
        }

        return Success<object?>(null, "Price removed from doctor successfully");
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Check if doctor email exists
    /// </summary>
    [HttpGet("validate/email/{email}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateDoctorEmail(string email, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _doctorService.DoctorEmailExistsAsync(email, excludeId);
        return Success<object>(new { exists }, "Email validation completed");
    }

    /// <summary>
    /// Check if doctor account exists
    /// </summary>
    [HttpGet("validate/account/{accountId}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateDoctorAccount(Guid accountId, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _doctorService.DoctorAccountExistsAsync(accountId, excludeId);
        return Success<object>(new { exists }, "Account validation completed");
    }

    /// <summary>
    /// Check if doctor-price relationship exists
    /// </summary>
    [HttpGet("validate/doctor-price")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateDoctorPrice([FromQuery] Guid doctorId, [FromQuery] Guid priceId)
    {
        var exists = await _doctorService.DoctorPriceExistsAsync(doctorId, priceId);
        return Success<object>(new { exists }, "Doctor-price relationship validation completed");
    }


    #endregion
}
