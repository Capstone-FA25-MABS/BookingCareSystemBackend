using System.Linq;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Models;
using Microsoft.AspNetCore.Authorization;
using BookingCare.Services.Hospital.Repositories.Interfaces;

namespace BookingCare.Services.Hospital.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[Authorize]
public class HospitalsController : BaseApiController
{
    private readonly IHospitalService _hospitalService;
    private readonly IHospitalRepository _hospitalRepository;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly IHospitalImageRepository _hospitalImageRepository;
    private readonly ILogger<HospitalsController> _logger;

    public HospitalsController(
        IHospitalService hospitalService,
        IHospitalRepository hospitalRepository,
        FileUploadOrchestrator uploadOrchestrator,
        IHospitalImageRepository hospitalImageRepository,
        ILogger<HospitalsController> logger)
    {
        _hospitalService = hospitalService;
        _hospitalRepository = hospitalRepository;
        _uploadOrchestrator = uploadOrchestrator;
        _hospitalImageRepository = hospitalImageRepository;
        _logger = logger;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Hospital", Timestamp = DateTime.UtcNow });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllHospitals([FromQuery] HospitalFilterRequest filter)
    {
        try
        {
            var result = await _hospitalService.GetFilteredAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospitals");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all hospitals (no pagination) - Optimized for performance
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllHospitalsSimple()
    {
        try
        {
            var hospitals = await _hospitalService.GetActiveHospitalsSimpleAsync();
            return Success<List<HospitalSimpleResponse>>(hospitals, "All active hospitals retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospitals");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get optimized hospital list with essential fields, filters, and pagination
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetOptimizedHospitalList([FromQuery] HospitalListOptimizedFilterRequest filter)
    {
        try
        {
            _logger.LogInformation("Controller received filter: Search={Search}, SpecialtyIds={SpecialtyIds}, ProvinceId={ProvinceId}, DistrictId={DistrictId}, Page={Page}, PageSize={PageSize}",
                filter.Search, string.Join(",", filter.SpecialtyIds ?? new string[0]), filter.ProvinceId, filter.DistrictId, filter.Page, filter.PageSize);

            var result = await _hospitalService.GetOptimizedHospitalListAsync(filter);
            return Success<HospitalListOptimizedPaginatedResponse>(result, "Hospital list retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving optimized hospital list");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHospitalById(Guid id)
    {
        try
        {
            var hospital = await _hospitalService.GetByIdAsync(id);
            if (hospital == null)
            {
                return NotFound(new { Message = $"Hospital with ID {id} not found" });
            }
            return Ok(hospital);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospital with ID {HospitalId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }


    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetHospitalByEmail(string email)
    {
        try
        {
            var hospital = await _hospitalService.GetByEmailAsync(email);
            if (hospital == null)
            {
                return NotFound(new { Message = $"Hospital with email {email} not found" });
            }
            return Ok(hospital);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospital with email {Email}", email);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("account")]
    public async Task<IActionResult> GetHospitalsByAccountId()
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            var hospitals = await _hospitalService.GetByAccountIdAsync(accountId);
            if (hospitals == null)
            {
                return NotFound("Hospital profile not found");
            }
            return Success(hospitals, "Hospital retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }

    }

    [HttpGet("specialty/{specialtyId}")]
    public async Task<IActionResult> GetHospitalsBySpecialty(Guid specialtyId)
    {
        try
        {
            var hospitals = await _hospitalService.GetBySpecialtyAsync(specialtyId);
            return Ok(hospitals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospitals for specialty {SpecialtyId}", specialtyId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalRequest request)
    {
        try
        {
            var hospital = await _hospitalService.CreateAsync(request);
            return CreatedAtAction(nameof(GetHospitalById), new { id = hospital.Id }, hospital);
        }
        catch (HospitalAlreadyExistsException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (InvalidHospitalDataException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hospital");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update hospital with avatar upload
    /// </summary>
    [HttpPut("{id}/upload-avatar")]
    public async Task<IActionResult> UpdateHospitalWithAvatar(
        Guid id,
        [FromForm] UpdateHospitalRequest request,
        [FromForm] IFormFile? avatarFile,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var currentHospital = await _hospitalService.GetByIdAsync(id);
            if (currentHospital == null)
            {
                return NotFound($"Hospital with ID {id} not found");
            }

            // Handle avatar upload if provided
            var uploadResult = await HandleAvatarUploadAsync(avatarFile, currentHospital, request, id, cancellationToken);
            if (uploadResult != null)
            {
                return uploadResult;
            }

            var hospital = await _hospitalService.UpdateAsync(id, request);
            return Ok(hospital);
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital with avatar");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update hospital with background upload
    /// </summary>
    [HttpPut("{id}/upload-background")]
    public async Task<IActionResult> UpdateHospitalWithBackground(
        Guid id,
        [FromForm] UpdateHospitalRequest request,
        [FromForm] IFormFile? backgroundFile,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var currentHospital = await _hospitalService.GetByIdAsync(id);
            if (currentHospital == null)
            {
                return NotFound($"Hospital with ID {id} not found");
            }

            // Handle background upload if provided
            var uploadResult = await HandleBackgroundUploadAsync(backgroundFile, currentHospital, request, id, cancellationToken);
            if (uploadResult != null)
            {
                return uploadResult;
            }

            var hospital = await _hospitalService.UpdateAsync(id, request);
            return Ok(hospital);
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital with background");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update hospital with both avatar and background upload
    /// </summary>
    [HttpPut("{id}/upload-files")]
    public async Task<IActionResult> UpdateHospitalWithFiles(
        Guid id,
        [FromForm] UpdateHospitalRequest request,
        [FromForm] IFormFile? avatarFile,
        [FromForm] IFormFile? backgroundFile,
        CancellationToken cancellationToken = default)
    {
        var validationResult = ValidateModelState();
        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var currentHospital = await _hospitalService.GetByIdAsync(id);
            if (currentHospital == null)
            {
                return NotFound($"Hospital with ID {id} not found");
            }

            // Handle avatar upload if provided
            var avatarUploadResult = await HandleAvatarUploadAsync(avatarFile, currentHospital, request, id, cancellationToken);
            if (avatarUploadResult != null)
            {
                return avatarUploadResult;
            }

            // Handle background upload if provided
            var backgroundUploadResult = await HandleBackgroundUploadAsync(backgroundFile, currentHospital, request, id, cancellationToken);
            if (backgroundUploadResult != null)
            {
                return backgroundUploadResult;
            }

            var hospital = await _hospitalService.UpdateAsync(id, request);
            return Ok(hospital);
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital with files");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Upload hospital images
    /// </summary>
    [HttpPost("{id}/images")]
    public async Task<IActionResult> UploadHospitalImages(
        Guid id,
        [FromForm] List<IFormFile> imageFiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentHospital = await _hospitalService.GetByIdAsync(id);
            if (currentHospital == null)
            {
                return NotFound($"Hospital with ID {id} not found");
            }

            if (imageFiles == null || imageFiles.Count == 0)
            {
                return BadRequest("No image files provided");
            }

            var uploadedImages = await ProcessImageUploadsAsync(imageFiles, id, currentHospital.AccountId, cancellationToken);

            return Ok(new { Images = uploadedImages, Message = $"{uploadedImages.Count} image(s) uploaded successfully" });
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading hospital images");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete hospital image
    /// </summary>
    [HttpDelete("{id}/images/{imageId}")]
    public async Task<IActionResult> DeleteHospitalImage(
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hospital = await _hospitalService.GetByIdAsync(id);
            if (hospital == null)
            {
                return NotFound($"Hospital with ID {id} not found");
            }

            var imageEntity = await _hospitalImageRepository.GetByIdAsync(imageId);
            if (imageEntity == null || imageEntity.HospitalId != id)
            {
                return NotFound(new { Message = "Hospital image not found" });
            }

            await DeleteImageFileFromStorageAsync(imageEntity.ImageUrl, id, imageId, cancellationToken);

            var result = await _hospitalService.DeleteHospitalImageAsync(id, imageId);
            if (result)
            {
                return Ok(new { Message = "Hospital image deleted successfully" });
            }
            return NotFound(new { Message = "Hospital image not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hospital image {ImageId} for hospital {HospitalId}", imageId, id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHospital(Guid id, [FromBody] UpdateHospitalRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new { Message = "Validation failed", Errors = errors });
        }

        try
        {
            var hospital = await _hospitalService.UpdateAsync(id, request);
            return Ok(hospital);
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (HospitalAlreadyExistsException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (InvalidHospitalDataException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital with ID {HospitalId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHospital(Guid id)
    {
        try
        {
            var result = await _hospitalService.DeleteAsync(id);
            if (result)
            {
                return NoContent();
            }
            return NotFound(new { Message = $"Hospital with ID {id} not found" });
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hospital with ID {HospitalId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost("{hospitalId}/specialties/{specialtyId}")]
    public async Task<IActionResult> AddSpecialtyToHospital(Guid hospitalId, Guid specialtyId)
    {
        try
        {
            var result = await _hospitalService.AddSpecialtyAsync(hospitalId, specialtyId);
            if (result)
            {
                return Ok(new { Message = "Specialty added successfully" });
            }
            return BadRequest(new { Message = "Failed to add specialty" });
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding specialty {SpecialtyId} to hospital {HospitalId}", specialtyId, hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpDelete("{hospitalId}/specialties/{specialtyId}")]
    public async Task<IActionResult> RemoveSpecialtyFromHospital(Guid hospitalId, Guid specialtyId)
    {
        try
        {
            var result = await _hospitalService.RemoveSpecialtyAsync(hospitalId, specialtyId);
            if (result)
            {
                return Ok(new { Message = "Specialty removed successfully" });
            }
            return BadRequest(new { Message = "Failed to remove specialty" });
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing specialty {SpecialtyId} from hospital {HospitalId}", specialtyId, hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    #region Helper Methods

    /// <summary>
    /// Validates ModelState and returns BadRequest if invalid
    /// </summary>
    private IActionResult? ValidateModelState()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(new { Message = "Validation failed", Errors = errors });
        }
        return null;
    }

    /// <summary>
    /// Handles avatar upload including deletion of old avatar
    /// </summary>
    private async Task<IActionResult?> HandleAvatarUploadAsync(
        IFormFile? avatarFile,
        HospitalProfileResponse currentHospital,
        UpdateHospitalRequest request,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        if (avatarFile == null) return null;

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(currentHospital.AvatarUrl))
        {
            var deleteConfig = new FileDeletionConfig
            {
                FileUrl = currentHospital.AvatarUrl,
                ExpectedFolder = "avatars",
                SuccessMessage = "Old avatar deleted successfully",
                EntityType = "hospital-avatar"
            };

            var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, currentHospital.AccountId, _logger, cancellationToken);
            if (!deleteResult.Success)
            {
                _logger.LogWarning("Failed to delete old avatar for hospital {HospitalId}: {Error}", hospitalId, deleteResult.ErrorMessage);
            }
        }

        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
            MaxSizeInMB = 5,
            Folder = "avatars/hospitals",
            SuccessMessage = "Hospital avatar uploaded successfully",
            EntityType = "hospital-avatar"
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(avatarFile, config, currentHospital.AccountId, _logger, cancellationToken);

        if (!uploadResult.Success)
        {
            return BadRequest($"Avatar upload failed: {uploadResult.ErrorMessage}");
        }

        // Set the avatar URL from upload result - use CloudFront URL for public access
        request.AvatarUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
        return null;
    }

    /// <summary>
    /// Handles background upload including deletion of old background
    /// </summary>
    private async Task<IActionResult?> HandleBackgroundUploadAsync(
        IFormFile? backgroundFile,
        HospitalProfileResponse currentHospital,
        UpdateHospitalRequest request,
        Guid hospitalId,
        CancellationToken cancellationToken)
    {
        if (backgroundFile == null) return null;

        // Delete old background if exists
        if (!string.IsNullOrEmpty(currentHospital.BackgroundUrl))
        {
            var deleteConfig = new FileDeletionConfig
            {
                FileUrl = currentHospital.BackgroundUrl,
                ExpectedFolder = "hospitals",
                SuccessMessage = "Old background deleted successfully",
                EntityType = "hospital-background"
            };

            var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, currentHospital.AccountId, _logger, cancellationToken);
            if (!deleteResult.Success)
            {
                _logger.LogWarning("Failed to delete old background for hospital {HospitalId}: {Error}", hospitalId, deleteResult.ErrorMessage);
            }
        }

        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
            MaxSizeInMB = 5,
            Folder = "hospitals/backgrounds",
            SuccessMessage = "Hospital background uploaded successfully",
            EntityType = "hospital-background"
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(backgroundFile, config, currentHospital.AccountId, _logger, cancellationToken);

        if (!uploadResult.Success)
        {
            return BadRequest($"Background upload failed: {uploadResult.ErrorMessage}");
        }

        // Set the background URL from upload result
        request.BackgroundUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
        return null;
    }

    /// <summary>
    /// Processes multiple image file uploads
    /// </summary>
    private async Task<List<HospitalImageResponse>> ProcessImageUploadsAsync(
        List<IFormFile> imageFiles,
        Guid hospitalId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var uploadedImages = new List<HospitalImageResponse>();

        foreach (var imageFile in imageFiles)
        {
            var image = await UploadSingleHospitalImageAsync(imageFile, hospitalId, accountId, cancellationToken);
            if (image != null)
            {
                uploadedImages.Add(image);
            }
        }

        return uploadedImages;
    }

    /// <summary>
    /// Uploads a single hospital image file
    /// </summary>
    private async Task<HospitalImageResponse?> UploadSingleHospitalImageAsync(
        IFormFile imageFile,
        Guid hospitalId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
            MaxSizeInMB = 5,
            Folder = "hospitals/hospital-images",
            SuccessMessage = "Hospital image uploaded successfully",
            EntityType = "hospital-image"
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(imageFile, config, accountId, _logger, cancellationToken);

        if (!uploadResult.Success)
        {
            _logger.LogWarning("Failed to upload image for hospital {HospitalId}: {Error}", hospitalId, uploadResult.ErrorMessage);
            return null;
        }

        var imageUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;

        var createImageRequest = new CreateHospitalImageRequest
        {
            HospitalId = hospitalId,
            ImageUrl = imageUrl
        };

        var image = await _hospitalService.AddHospitalImageAsync(createImageRequest);
        if (image == null)
        {
            return null;
        }

        return new HospitalImageResponse
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl
        };
    }

    /// <summary>
    /// Deletes image file from storage (S3)
    /// </summary>
    private async Task DeleteImageFileFromStorageAsync(
        string? imageUrl,
        Guid hospitalId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return;
        }

        var hospitalEntity = await _hospitalRepository.GetByIdAsync(hospitalId);
        if (hospitalEntity == null)
        {
            return;
        }

        var deleteConfig = new FileDeletionConfig
        {
            FileUrl = imageUrl,
            ExpectedFolder = "hospitals",
            SuccessMessage = "Hospital image deleted successfully",
            EntityType = "hospital-image"
        };

        var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, hospitalEntity.AccountId, _logger, cancellationToken);
        if (!deleteResult.Success)
        {
            _logger.LogWarning("Failed to delete hospital image file from S3 for image {ImageId}: {Error}", imageId, deleteResult.ErrorMessage);
        }
    }

    #endregion
}