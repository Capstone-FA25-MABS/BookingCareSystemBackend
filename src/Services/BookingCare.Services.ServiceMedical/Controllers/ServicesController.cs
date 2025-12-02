using BookingCare.Services.ServiceMedical.Constants;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.ServiceMedical.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class ServicesController : ControllerBase
    {
        private readonly IServiceMedicalService _serviceMedicalService;
        private readonly FileUploadOrchestrator _uploadOrchestrator;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(
            IServiceMedicalService serviceMedicalService,
            FileUploadOrchestrator uploadOrchestrator,
            ILogger<ServicesController> logger)
        {
            _serviceMedicalService = serviceMedicalService;
            _uploadOrchestrator = uploadOrchestrator;
            _logger = logger;
        }

        #region Health Check

        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>Health status</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult Health()
        {
            return Ok(new
            {
                Status = "Healthy",
                Service = "ServiceMedical - Services",
                Timestamp = DateTime.UtcNow
            });
        }

        #endregion

        #region Service CRUD Operations

        /// <summary>
        /// Create a new service
        /// </summary>
        /// <param name="request">Service creation request</param>
        /// <returns>Created service</returns>
        [HttpPost]
        public async Task<ActionResult<ServiceResponse>> CreateService([FromBody] CreateServiceRequest request)
        {
            try
            {
                var result = await _serviceMedicalService.CreateServiceAsync(request);
                return CreatedAtAction(nameof(GetService), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating service: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Create a new service with image upload
        /// </summary>
        /// <param name="request">Service creation request</param>
        /// <param name="imageFile">Service image file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Created service</returns>
        [HttpPost("upload-image")]
        // [Authorize] // Temporarily disabled for testing - enable after authentication is configured
        public async Task<ActionResult<ServiceResponse>> CreateServiceWithImage(
            [FromForm] CreateServiceRequest request,
            [FromForm] IFormFile? imageFile,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Log received data for debugging
                _logger.LogInformation("Received CreateServiceWithImage: Name={Name}, Price={Price}, HospitalId={HospitalId}, ServiceCategoryId={ServiceCategoryId}, DurationTime={DurationTime}, HasImage={HasImage}",
                    request?.Name, request?.Price, request?.HospitalId, request?.ServiceCategoryId, request?.DurationTime, imageFile != null);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    _logger.LogWarning("ModelState invalid: {Errors}", string.Join(", ", errors));
                    return BadRequest(new { error = "Invalid request data", errors = errors });
                }

                // Validate required fields
                if (request == null)
                {
                    _logger.LogError("CreateServiceRequest is null");
                    return BadRequest(new { error = "Request data is required" });
                }

                // Handle image upload if provided
                if (imageFile != null)
                {
                    var config = new FileUploadConfig
                    {
                        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                        MaxSizeInMB = 5,
                        Folder = "services/images",
                        SuccessMessage = "Service image uploaded successfully",
                        EntityType = "service-image"
                    };

                    var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                        imageFile,
                        config,
                        request.HospitalId,
                        _logger,
                        cancellationToken);

                    if (!uploadResult.Success)
                    {
                        return BadRequest(new { error = $"Image upload failed: {uploadResult.ErrorMessage}" });
                    }

                    // Set the image URL from upload result - use CloudFront URL for public access
                    request.ImageUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
                }

                var result = await _serviceMedicalService.CreateServiceAsync(request);

                return CreatedAtAction(nameof(GetService), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "ArgumentException when creating service: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating service with image: {Message}", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service with image: {Message}. StackTrace: {StackTrace}",
                    ex.Message, ex.StackTrace);
                return StatusCode(500, new
                {
                    error = StatusConstants.InternalServerError,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Get service by ID
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <returns>Service details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceResponse>> GetService(Guid id)
        {
            try
            {
                var result = await _serviceMedicalService.GetServiceByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { error = $"Service with ID {id} not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service by ID: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get service by ID with hospital information
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <returns>Service details with hospital information</returns>
        [HttpGet("{id}/with-hospital")]
        public async Task<ActionResult<ServiceWithHospitalResponse>> GetServiceWithHospital(Guid id)
        {
            try
            {
                var result = await _serviceMedicalService.GetServiceWithHospitalByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { error = $"Service with ID {id} not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service with hospital by ID: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Update service
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <param name="request">Service update request</param>
        /// <returns>Updated service</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ServiceResponse>> UpdateService(Guid id, [FromBody] UpdateServiceRequest request)
        {
            try
            {
                if (id != request.Id)
                {
                    return BadRequest(new { error = "ID mismatch" });
                }

                var result = await _serviceMedicalService.UpdateServiceAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Update service with image upload
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <param name="request">Service update request</param>
        /// <param name="imageFile">Service image file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated service</returns>
        [HttpPut("{id}/upload-image")]
        // [Authorize] // Temporarily disabled for testing - enable after authentication is configured
        public async Task<ActionResult<ServiceResponse>> UpdateServiceWithImage(
            Guid id,
            [FromForm] UpdateServiceRequest request,
            [FromForm] IFormFile? imageFile,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (id != request.Id)
                {
                    return BadRequest(new { error = "ID mismatch" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new { error = "Invalid request data", errors = ModelState });
                }

                // Get current service to get hospital ID for image upload
                var currentService = await _serviceMedicalService.GetServiceByIdAsync(id);
                if (currentService == null)
                {
                    return NotFound(new { error = $"Service with ID {id} not found" });
                }

                // Handle image upload if provided
                if (imageFile != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(currentService.ImageUrl))
                    {
                        var deleteConfig = new FileDeletionConfig
                        {
                            FileUrl = currentService.ImageUrl,
                            ExpectedFolder = "services",
                            SuccessMessage = "Old service image deleted successfully",
                            EntityType = "service-image"
                        };

                        var deleteResult = await _uploadOrchestrator.DeleteFileAsync(
                            deleteConfig,
                            currentService.HospitalId,
                            _logger,
                            cancellationToken);

                        if (!deleteResult.Success)
                        {
                            _logger.LogWarning("Failed to delete old image for service {ServiceId}: {Error}", id, deleteResult.ErrorMessage);
                            // Continue with upload even if deletion fails
                        }
                    }

                    var config = new FileUploadConfig
                    {
                        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                        MaxSizeInMB = 5,
                        Folder = "services/images",
                        SuccessMessage = "Service image uploaded successfully",
                        EntityType = "service-image"
                    };

                    var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                        imageFile,
                        config,
                        currentService.HospitalId,
                        _logger,
                        cancellationToken);

                    if (!uploadResult.Success)
                    {
                        return BadRequest(new { error = $"Image upload failed: {uploadResult.ErrorMessage}" });
                    }

                    // Set the image URL from upload result - use CloudFront URL for public access
                    request.ImageUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
                }

                var result = await _serviceMedicalService.UpdateServiceAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service with image: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Delete service (soft delete - changes status to INACTIVE)
        /// </summary>
        /// <param name="id">Service ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            try
            {
                var success = await _serviceMedicalService.DeleteServiceAsync(id);
                if (!success)
                {
                    return NotFound(new { error = $"Service with ID {id} not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        #endregion

        #region Service Query Operations

        /// <summary>
        /// Get services with pagination and filtering
        /// </summary>
        /// <param name="query">Service query parameters</param>
        /// <returns>Paginated list of services</returns>
        [HttpGet]
        public async Task<ActionResult<ServiceListResponse>> GetServices([FromQuery] ServiceQueryRequest query)
        {
            try
            {
                var result = await _serviceMedicalService.GetServicesAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get services by category
        /// </summary>
        /// <param name="categoryId">Service category ID</param>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="includeInactive">Include inactive services</param>
        /// <returns>List of services in the category</returns>
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<ServiceListResponse>> GetServicesByCategory(
            Guid categoryId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var request = new GetServicesByCategoryRequest
                {
                    ServiceCategoryId = categoryId,
                    Page = page,
                    PageSize = pageSize,
                    IncludeInactive = includeInactive
                };

                var result = await _serviceMedicalService.GetServicesByCategoryAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by category: {CategoryId}", categoryId);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get services by hospital
        /// </summary>
        /// <param name="hospitalId">Hospital ID</param>
        /// <returns>List of services offered by the hospital</returns>
        [HttpGet("hospital/{hospitalId}")]
        public async Task<ActionResult<List<ServiceResponse>>> GetServicesByHospital(Guid hospitalId)
        {
            try
            {
                var result = await _serviceMedicalService.GetServicesByHospitalAsync(hospitalId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by hospital: {HospitalId}", hospitalId);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get all active services
        /// </summary>
        /// <returns>List of active services</returns>
        [HttpGet("active")]
        public async Task<ActionResult<List<ServiceResponse>>> GetActiveServices()
        {
            try
            {
                var result = await _serviceMedicalService.GetActiveServicesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active services");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get services by category with hospital information
        /// </summary>
        /// <param name="categoryId">Service category ID</param>
        /// <param name="queryParams">Query parameters for filtering and pagination</param>
        /// <returns>List of services in the category with hospital information</returns>
        [HttpGet("category/{categoryId}/with-hospital")]
        public async Task<ActionResult<ServicesByCategoryOptimizedResponse>> GetServicesByCategoryWithHospital(
            Guid categoryId,
            [FromQuery] GetServicesByCategoryWithHospitalQueryParams queryParams)
        {
            try
            {
                // Validate model state to ensure ProvinceId and DistrictId meet security requirements
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return BadRequest(new { error = "Invalid request parameters", errors = errors });
                }

                // Additional validation for ProvinceId and DistrictId to prevent path traversal
                if (!string.IsNullOrWhiteSpace(queryParams.ProvinceId) && !IsValidLocationId(queryParams.ProvinceId))
                {
                    return BadRequest(new { error = "Invalid ProvinceId format. Only alphanumeric characters, hyphens, and underscores are allowed." });
                }

                if (!string.IsNullOrWhiteSpace(queryParams.DistrictId) && !IsValidLocationId(queryParams.DistrictId))
                {
                    return BadRequest(new { error = "Invalid DistrictId format. Only alphanumeric characters, hyphens, and underscores are allowed." });
                }

                var request = new GetServicesByCategoryRequest
                {
                    ServiceCategoryId = categoryId,
                    Page = queryParams.Page,
                    PageSize = queryParams.PageSize,
                    IncludeInactive = queryParams.IncludeInactive,
                    SearchTerm = queryParams.SearchTerm,
                    ProvinceId = queryParams.ProvinceId,
                    DistrictId = queryParams.DistrictId
                };

                // Parse hospital IDs from comma-separated string
                if (!string.IsNullOrEmpty(queryParams.HospitalIds))
                {
                    var hospitalIdList = queryParams.HospitalIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : (Guid?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .ToList();

                    if (hospitalIdList.Any())
                    {
                        request.HospitalIds = hospitalIdList;
                    }
                }

                var result = await _serviceMedicalService.GetServicesByCategoryWithHospitalAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting services by category with hospital info: {CategoryId}", categoryId);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Validate location ID format to prevent path traversal attacks
        /// Only allows alphanumeric characters, hyphens, and underscores
        /// </summary>
        private static bool IsValidLocationId(string locationId)
        {
            if (string.IsNullOrWhiteSpace(locationId))
            {
                return false;
            }

            // Allow only alphanumeric characters, hyphens, and underscores
            // This prevents path traversal characters like ../, ..\, etc.
            return locationId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_') &&
                   locationId.Length <= 50; // Reasonable length limit
        }

        /// <summary>
        /// Get all services with details (id, name, description, price, duration, hospital name, category name, status)
        /// Supports filtering and sorting
        /// </summary>
        /// <param name="query">Query parameters for filtering, sorting, and pagination</param>
        /// <returns>List of services with detailed information</returns>
        [HttpGet("all-details")]
        public async Task<ActionResult<ServiceDetailListResponse>> GetAllServicesWithDetails([FromQuery] ServiceQueryRequest? query)
        {
            try
            {
                var result = await _serviceMedicalService.GetAllServicesWithDetailsAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all services with details");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get filter options for dropdown (hospitals and service categories)
        /// Returns list of hospitals (id, name) and list of child service categories (id, name)
        /// </summary>
        /// <returns>Filter options with hospitals and service categories</returns>
        [HttpGet("filter-options")]
        public async Task<ActionResult<FilterOptionsResponse>> GetFilterOptions()
        {
            try
            {
                var result = await _serviceMedicalService.GetFilterOptionsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filter options");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }


        #endregion
    }
}
