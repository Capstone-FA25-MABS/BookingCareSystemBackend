using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class SpecialtiesController : BaseApiController
{
    private readonly ISpecialtyService _specialtyService;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly ILogger<SpecialtiesController> _logger;

    public SpecialtiesController(
        ISpecialtyService specialtyService,
        FileUploadOrchestrator uploadOrchestrator,
        ILogger<SpecialtiesController> logger)
    {
        _specialtyService = specialtyService;
        _uploadOrchestrator = uploadOrchestrator;
        _logger = logger;
    }

    #region Private Helper Methods

    /// <summary>
    /// Handle specialty image upload
    /// </summary>
    /// <param name="imageFile">Image file to upload</param>
    /// <param name="request">Request object to set image URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BadRequest if upload fails, null if successful</returns>
    private async Task<IActionResult?> HandleSpecialtyImageUploadAsync(IFormFile? imageFile, dynamic request, CancellationToken cancellationToken)
    {
        if (imageFile == null) return null;

        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
            MaxSizeInMB = 5,
            Folder = "specialties",
            SuccessMessage = "Specialty image uploaded successfully",
            EntityType = "specialty-image"
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(imageFile, config, Guid.Empty, _logger, cancellationToken);

        if (!uploadResult.Success)
        {
            return BadRequest($"Tải lên hình ảnh thất bại: {uploadResult.ErrorMessage}");
        }

        // Set the image URL from upload result - use CloudFront URL for public access
        request.ImageUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
        return null;
    }

    #endregion

    #region Health Check

    /// <summary>
    /// Health check endpoint - Available in all versions
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Specialties",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Specialty Endpoints

    /// <summary>
    /// Get specialty by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetSpecialty(Guid id)
    {
        var specialty = await _specialtyService.GetSpecialtyByIdAsync(id);
        if (specialty == null)
        {
            return NotFound($"Không tìm thấy chuyên khoa với ID {id}");
        }

        return Success<SpecialtyResponse>(specialty, "Lấy thông tin chuyên khoa thành công");
    }

    /// <summary>
    /// Get specialty by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetSpecialtyByName(string name)
    {
        var specialty = await _specialtyService.GetSpecialtyByNameAsync(name);
        if (specialty == null)
        {
            return NotFound($"Không tìm thấy chuyên khoa với tên '{name}'");
        }

        return Success<SpecialtyResponse>(specialty, "Lấy thông tin chuyên khoa thành công");
    }

    /// <summary>
    /// Get all specialties
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetSpecialties([FromQuery] SpecialtyQueryRequest query)
    {
        var result = await _specialtyService.GetSpecialtiesAsync(query);
        return Success<SpecialtyListResponse>(result, "Lấy danh sách chuyên khoa thành công");
    }

    /// <summary>
    /// Get all specialties (no pagination) - Optimized for performance
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllSpecialties()
    {
        var specialties = await _specialtyService.GetActiveSpecialtiesSimpleAsync();
        return Success<List<SpecialtySimpleResponse>>(specialties, "Lấy tất cả chuyên khoa hoạt động thành công");
    }

    /// <summary>
    /// Get active specialties only
    /// </summary>
    [HttpGet("active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveSpecialties()
    {
        var specialties = await _specialtyService.GetActiveSpecialtiesAsync();
        return Success<List<SpecialtyResponse>>(specialties, "Lấy danh sách chuyên khoa hoạt động thành công");
    }

    /// <summary>
    /// Create a new specialty
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateSpecialty([FromBody] CreateSpecialtyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu yêu cầu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var specialty = await _specialtyService.CreateSpecialtyAsync(request);
        return Created(specialty, "Tạo chuyên khoa thành công");
    }

    /// <summary>
    /// Create a new specialty with image upload
    /// </summary>
    [HttpPost("upload-image")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateSpecialtyWithImage(
        [FromForm] CreateSpecialtyWithImageRequest request,
        [FromForm] IFormFile? imageFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            // Handle image upload if provided
            var uploadError = await HandleSpecialtyImageUploadAsync(imageFile, request, cancellationToken);
            if (uploadError != null) return uploadError;

            // Convert to CreateSpecialtyRequest for service layer
            var createRequest = new CreateSpecialtyRequest
            {
                Name = request.Name,
                ImageUrl = request.ImageUrl ?? string.Empty,
                Status = request.Status
            };

            var specialty = await _specialtyService.CreateSpecialtyAsync(createRequest);
            return Created(specialty, "Tạo chuyên khoa với hình ảnh thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo chuyên khoa với hình ảnh: {Message}", ex.Message);
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
    /// Update specialty
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateSpecialty(Guid id, [FromBody] UpdateSpecialtyRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu yêu cầu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var specialty = await _specialtyService.UpdateSpecialtyAsync(request);
        return Success<SpecialtyResponse>(specialty, "Cập nhật chuyên khoa thành công");
    }

    /// <summary>
    /// Update specialty with image upload
    /// </summary>
    [HttpPut("{id}/upload-image")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateSpecialtyWithImage(
        Guid id,
        [FromForm] UpdateSpecialtyWithImageRequest request,
        [FromForm] IFormFile? imageFile,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request data", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList());
            }

            request.Id = id;

            // Handle image upload if provided
            if (imageFile != null)
            {
                // Get current specialty to check for existing image
                var currentSpecialty = await _specialtyService.GetSpecialtyByIdAsync(id);
                if (currentSpecialty != null && !string.IsNullOrEmpty(currentSpecialty.ImageUrl))
                {
                    // Delete old image from S3
                    var deleteConfig = new FileDeletionConfig
                    {
                        FileUrl = currentSpecialty.ImageUrl,
                        ExpectedFolder = "specialties",
                        EntityType = "specialty-image"
                    };

                    var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, Guid.Empty, _logger, cancellationToken);
                    if (!deleteResult.Success)
                    {
                        _logger.LogWarning("Failed to delete old specialty image: {ErrorMessage}", deleteResult.ErrorMessage);
                        // Continue with upload even if deletion fails
                    }
                }

                var uploadError = await HandleSpecialtyImageUploadAsync(imageFile, request, cancellationToken);
                if (uploadError != null) return uploadError;
            }

            // Convert to UpdateSpecialtyRequest for service layer
            var updateRequest = new UpdateSpecialtyRequest
            {
                Id = id,
                Name = request.Name,
                ImageUrl = request.ImageUrl ?? string.Empty,
                Status = request.Status
            };

            var specialty = await _specialtyService.UpdateSpecialtyAsync(updateRequest);
            return Success<SpecialtyResponse>(specialty, "Cập nhật chuyên khoa với hình ảnh thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi cập nhật chuyên khoa với hình ảnh: {Message}", ex.Message);
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
    /// Delete specialty
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteSpecialty(Guid id)
    {
        try
        {
            // Get specialty before deletion to get image URL
            var specialty = await _specialtyService.GetSpecialtyByIdAsync(id);
            if (specialty == null)
            {
                return NotFound($"Không tìm thấy chuyên khoa với ID {id}");
            }

            // Delete the specialty from database
            var result = await _specialtyService.DeleteSpecialtyAsync(id);
            if (!result)
            {
                return NotFound($"Không tìm thấy chuyên khoa với ID {id}");
            }

            // Delete associated image from S3 if exists
            if (!string.IsNullOrEmpty(specialty.ImageUrl))
            {
                var deleteConfig = new FileDeletionConfig
                {
                    FileUrl = specialty.ImageUrl,
                    ExpectedFolder = "specialties",
                    EntityType = "specialty-image"
                };

                var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, Guid.Empty, _logger, CancellationToken.None);
                if (!deleteResult.Success)
                {
                    _logger.LogWarning("Failed to delete specialty image from S3: {ErrorMessage}", deleteResult.ErrorMessage);
                    // Continue even if image deletion fails
                }
            }

            return Success<object?>(null, "Xóa chuyên khoa thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi xóa chuyên khoa: {Message}", ex.Message);
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
    /// Toggle specialty status (ACTIVE/INACTIVE)
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ToggleSpecialtyStatus(Guid id)
    {
        var result = await _specialtyService.ToggleSpecialtyStatusAsync(id);
        if (!result)
        {
            return NotFound($"Không tìm thấy chuyên khoa với ID {id}");
        }

        return Success<object?>(null, "Thay đổi trạng thái chuyên khoa thành công");
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Check if specialty name exists
    /// </summary>
    [HttpGet("validate/name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateSpecialtyName(string name, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _specialtyService.SpecialtyNameExistsAsync(name, excludeId);
        return Success<object>(new { exists }, "Kiểm tra tên chuyên khoa hoàn tất");
    }

    #endregion
}
