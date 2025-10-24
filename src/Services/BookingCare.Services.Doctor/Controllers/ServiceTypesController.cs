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
public class ServiceTypesController : BaseApiController
{
    private readonly IServiceTypeService _serviceTypeService;
    private readonly FileUploadOrchestrator _uploadOrchestrator;
    private readonly ILogger<ServiceTypesController> _logger;

    public ServiceTypesController(
        IServiceTypeService serviceTypeService,
        FileUploadOrchestrator uploadOrchestrator,
        ILogger<ServiceTypesController> logger)
    {
        _serviceTypeService = serviceTypeService;
        _uploadOrchestrator = uploadOrchestrator;
        _logger = logger;
    }

    #region Private Helper Methods

    /// <summary>
    /// Handle service type image upload
    /// </summary>
    /// <param name="imageFile">Image file to upload</param>
    /// <param name="request">Request object to set image URL</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BadRequest if upload fails, null if successful</returns>
    private async Task<IActionResult?> HandleServiceTypeImageUploadAsync(IFormFile? imageFile, dynamic request, CancellationToken cancellationToken)
    {
        if (imageFile == null) return null;

        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
            MaxSizeInMB = 5,
            Folder = "service-types",
            SuccessMessage = "Service type image uploaded successfully",
            EntityType = "service-type-image"
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
            Service = "ServiceTypes",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region ServiceType Endpoints

    /// <summary>
    /// Get service type by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetServiceType(Guid id)
    {
        var serviceType = await _serviceTypeService.GetServiceTypeByIdAsync(id);
        if (serviceType == null)
        {
            return NotFound("Không tìm thấy loại dịch vụ");
        }
        return Success<ServiceTypeResponse>(serviceType, "Lấy thông tin loại dịch vụ thành công");
    }

    /// <summary>
    /// Get all service types with pagination
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetServiceTypes([FromQuery] ServiceTypeQueryRequest query)
    {
        var serviceTypes = await _serviceTypeService.GetServiceTypesAsync(query);
        return Success<ServiceTypeListResponse>(serviceTypes, "Lấy danh sách loại dịch vụ thành công");
    }

    /// <summary>
    /// Get all service types
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllServiceTypes()
    {
        var serviceTypes = await _serviceTypeService.GetAllServiceTypesAsync();
        return Success<List<ServiceTypeResponse>>(serviceTypes, "Lấy tất cả loại dịch vụ thành công");
    }

    /// <summary>
    /// Get active service types
    /// </summary>
    [HttpGet("active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveServiceTypes()
    {
        var serviceTypes = await _serviceTypeService.GetActiveServiceTypesAsync();
        return Success<List<ServiceTypeResponse>>(serviceTypes, "Lấy danh sách loại dịch vụ hoạt động thành công");
    }

    /// <summary>
    /// Create a new service type
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateServiceType([FromBody] CreateServiceTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu yêu cầu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var serviceType = await _serviceTypeService.CreateServiceTypeAsync(request);
        return Created(serviceType, "Tạo loại dịch vụ thành công");
    }

    /// <summary>
    /// Create a new service type with image upload
    /// </summary>
    [HttpPost("upload-image")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateServiceTypeWithImage(
        [FromForm] CreateServiceTypeWithImageRequest request,
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
            var uploadError = await HandleServiceTypeImageUploadAsync(imageFile, request, cancellationToken);
            if (uploadError != null) return uploadError;

            // Convert to CreateServiceTypeRequest for service layer
            var createRequest = new CreateServiceTypeRequest
            {
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl ?? string.Empty,
                Status = request.Status
            };

            var serviceType = await _serviceTypeService.CreateServiceTypeAsync(createRequest);
            return Created(serviceType, "Tạo loại dịch vụ với hình ảnh thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo loại dịch vụ với hình ảnh: {Message}", ex.Message);
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
    /// Update service type
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateServiceType(Guid id, [FromBody] UpdateServiceTypeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu yêu cầu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        // Get current service type to preserve existing image if no new image is provided
        var currentServiceType = await _serviceTypeService.GetServiceTypeByIdAsync(id);
        if (currentServiceType == null)
        {
            return NotFound("Không tìm thấy loại dịch vụ");
        }

        // Prepare update request - keep existing imageUrl if no new one is provided
        var updateRequest = new UpdateServiceTypeRequest
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            ImageUrl = !string.IsNullOrEmpty(request.ImageUrl) ? request.ImageUrl : currentServiceType.ImageUrl,
            Status = request.Status
        };

        var serviceType = await _serviceTypeService.UpdateServiceTypeAsync(updateRequest);
        return Success<ServiceTypeResponse>(serviceType, "Cập nhật loại dịch vụ thành công");
    }

    /// <summary>
    /// Update service type with image upload
    /// </summary>
    [HttpPut("{id}/upload-image")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateServiceTypeWithImage(
        Guid id,
        [FromForm] UpdateServiceTypeWithImageRequest request,
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
                // Get current service type to check for existing image
                var currentServiceType = await _serviceTypeService.GetServiceTypeByIdAsync(id);
                if (currentServiceType != null && !string.IsNullOrEmpty(currentServiceType.ImageUrl))
                {
                    // Delete old image from S3
                    var deleteConfig = new FileDeletionConfig
                    {
                        FileUrl = currentServiceType.ImageUrl,
                        ExpectedFolder = "service-types",
                        EntityType = "service-type-image"
                    };

                    var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, Guid.Empty, _logger, cancellationToken);
                    if (!deleteResult.Success)
                    {
                        _logger.LogWarning("Failed to delete old service type image: {ErrorMessage}", deleteResult.ErrorMessage);
                        // Continue with upload even if deletion fails
                    }
                }

                var uploadError = await HandleServiceTypeImageUploadAsync(imageFile, request, cancellationToken);
                if (uploadError != null) return uploadError;
            }

            // Convert to UpdateServiceTypeRequest for service layer
            var updateRequest = new UpdateServiceTypeRequest
            {
                Id = id,
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl ?? string.Empty,
                Status = request.Status
            };

            var serviceType = await _serviceTypeService.UpdateServiceTypeAsync(updateRequest);
            return Success<ServiceTypeResponse>(serviceType, "Cập nhật loại dịch vụ với hình ảnh thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi cập nhật loại dịch vụ với hình ảnh: {Message}", ex.Message);
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
    /// Delete service type
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteServiceType(Guid id)
    {
        var result = await _serviceTypeService.DeleteServiceTypeAsync(id);
        if (!result)
        {
            return NotFound("Không tìm thấy loại dịch vụ");
        }
        return Success("Xóa loại dịch vụ thành công");
    }

    /// <summary>
    /// Toggle service type status
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ToggleServiceTypeStatus(Guid id)
    {
        var result = await _serviceTypeService.ToggleServiceTypeStatusAsync(id);
        if (!result)
        {
            return NotFound("Không tìm thấy loại dịch vụ");
        }
        return Success("Chuyển đổi trạng thái loại dịch vụ thành công");
    }

    #endregion
}