using AutoMapper;
using BookingCare.Services.ServiceMedical.Constants;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Models;

namespace BookingCare.Services.ServiceMedical.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class ServiceCategoriesController : ControllerBase
    {
        private readonly IServiceMedicalService _serviceMedicalService;
        private readonly FileUploadOrchestrator _uploadOrchestrator;
        private readonly ILogger<ServiceCategoriesController> _logger;
        private readonly IMapper _mapper;

        public ServiceCategoriesController(IServiceMedicalService serviceMedicalService, FileUploadOrchestrator uploadOrchestrator, ILogger<ServiceCategoriesController> logger, IMapper mapper)
        {
            _serviceMedicalService = serviceMedicalService;
            _uploadOrchestrator = uploadOrchestrator;
            _logger = logger;
            _mapper = mapper;
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
                Service = "ServiceMedical - ServiceCategories",
                Timestamp = DateTime.UtcNow
            });
        }

        #endregion

        #region ServiceCategory CRUD Operations

        /// <summary>
        /// Create a new service category (Admin only)
        /// </summary>
        /// <param name="request">Service category creation request</param>
        /// <returns>Created service category</returns>
        [HttpPost]
        [Authorize(Policy = "Role:Admin")]
        public async Task<ActionResult<ServiceCategoryResponse>> CreateServiceCategory([FromBody] CreateServiceCategoryRequest request)
        {
            try
            {
                var result = await _serviceMedicalService.CreateServiceCategoryAsync(request);
                return CreatedAtAction(nameof(GetServiceCategory), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service category");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get service category by ID
        /// </summary>
        /// <param name="id">Service category ID</param>
        /// <returns>Service category details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ServiceCategoryResponse>> GetServiceCategory(Guid id)
        {
            try
            {
                var result = await _serviceMedicalService.GetServiceCategoryByIdAsync(id);
                if (result == null)
                {
                    return NotFound(new { error = $"Service category with ID {id} not found" });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service category by ID: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Update service category (Admin only)
        /// </summary>
        /// <param name="id">Service category ID</param>
        /// <param name="request">Service category update request</param>
        /// <returns>Updated service category</returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "Role:Admin")]
        public async Task<ActionResult<ServiceCategoryResponse>> UpdateServiceCategory(Guid id, [FromBody] UpdateServiceCategoryRequest request)
        {
            try
            {
                if (id != request.Id)
                {
                    return BadRequest(new { error = "ID mismatch" });
                }

                var result = await _serviceMedicalService.UpdateServiceCategoryAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service category: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Delete service category (soft delete - changes status to INACTIVE, Admin only)
        /// </summary>
        /// <param name="id">Service category ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "Role:Admin")]
        public async Task<IActionResult> DeleteServiceCategory(Guid id)
        {
            try
            {
                var success = await _serviceMedicalService.DeleteServiceCategoryAsync(id);
                if (!success)
                {
                    return NotFound(new { error = $"Service category with ID {id} not found" });
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service category: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        #endregion

        #region ServiceCategory Query Operations - Theo luồng bạn yêu cầu

        /// <summary>
        /// Get all parent service categories (luồng 1: List parent categories)
        /// </summary>
        /// <returns>List of parent service categories</returns>
        [HttpGet("parents")]
        public async Task<ActionResult<List<ServiceCategoryResponse>>> GetParentServiceCategories()
        {
            try
            {
                var result = await _serviceMedicalService.GetParentServiceCategoriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parent service categories");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get children of a parent service category (luồng 2: List children when click parent)
        /// </summary>
        /// <param name="parentId">Parent service category ID</param>
        /// <param name="includeInactive">Include inactive categories</param>
        /// <returns>List of child service categories</returns>
        [HttpGet("{parentId}/children")]
        public async Task<ActionResult<List<ServiceCategoryResponse>>> GetServiceCategoryChildren(
            Guid parentId,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var request = new GetServiceCategoryChildrenRequest
                {
                    ParentId = parentId,
                    IncludeInactive = includeInactive
                };

                var result = await _serviceMedicalService.GetServiceCategoryChildrenAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service category children for parent: {ParentId}", parentId);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get hospitals by service category (luồng 3: List hospital IDs for a service category)
        /// </summary>
        /// <param name="categoryId">Service category ID</param>
        /// <param name="includeInactive">Include inactive services</param>
        /// <returns>List of hospital IDs that have services in this category</returns>
        [HttpGet("{categoryId}/hospitals")]
        public async Task<ActionResult<HospitalsByServiceCategoryResponse>> GetHospitalsByServiceCategory(
            Guid categoryId,
            [FromQuery] bool includeInactive = false)
        {
            try
            {
                var request = new GetHospitalsByServiceCategoryRequest
                {
                    ServiceCategoryId = categoryId,
                    IncludeInactive = includeInactive
                };

                var result = await _serviceMedicalService.GetHospitalsByServiceCategoryAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hospitals by service category: {CategoryId}", categoryId);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get service categories with pagination and filtering
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="searchTerm">Search term</param>
        /// <param name="status">Status filter</param>
        /// <param name="parentId">Parent ID filter</param>
        /// <param name="includeChildren">Include children in response</param>
        /// <returns>Paginated list of service categories</returns>
        [HttpGet]
        public async Task<ActionResult<ServiceCategoryListResponse>> GetServiceCategories(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? parentId = null,
            [FromQuery] bool includeChildren = false)
        {
            try
            {
                var request = new ServiceCategoryQueryRequest
                {
                    Page = page,
                    PageSize = pageSize,
                    SearchTerm = searchTerm,
                    Status = status,
                    ParentId = parentId,
                    IncludeChildren = includeChildren
                };

                var result = await _serviceMedicalService.GetServiceCategoriesAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service categories");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get all service categories with details (flat list with filtering and sorting)
        /// Returns all categories (parent and child) in a flat list format - no pagination
        /// </summary>
        /// <param name="searchTerm">Search term for filtering by name or description</param>
        /// <param name="status">Status filter (ACTIVE or INACTIVE)</param>
        /// <param name="parentId">Filter by parent ID (null for parent categories only)</param>
        /// <param name="sortBy">Sort field: "Name" (default)</param>
        /// <param name="sortDirection">Sort direction: "asc" (A-Z) or "desc" (Z-A). Default: "asc"</param>
        /// <returns>List of all service categories with details</returns>
        [HttpGet("all-details")]
        public async Task<ActionResult<ServiceCategoryAdminListResponse>> GetAllServiceCategoriesWithDetails(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? parentId = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = "asc")
        {
            try
            {
                // Get all categories without pagination
                var request = new ServiceCategoryQueryRequest
                {
                    Page = 1,
                    PageSize = int.MaxValue, // Get all items
                    SearchTerm = searchTerm,
                    Status = status,
                    ParentId = parentId,
                    SortBy = sortBy,
                    SortDirection = sortDirection
                };

                var result = await _serviceMedicalService.GetServiceCategoriesAsync(request);

                // Convert to admin response format (map to ServiceCategoryAdminResponse to avoid navigation properties)
                var adminResponse = new ServiceCategoryAdminListResponse
                {
                    ServiceCategories = _mapper.Map<List<ServiceCategoryAdminResponse>>(result.Categories),
                    TotalCount = result.TotalCount
                };

                return Ok(adminResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all service categories with details");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Create a new service category with image upload (Admin only)
        /// </summary>
        [HttpPost("upload-image")]
        [Authorize(Policy = "Role:Admin")]
        public async Task<ActionResult<ServiceCategoryResponse>> CreateServiceCategoryWithImage(
            [FromForm] CreateServiceCategoryRequest request,
            [FromForm] IFormFile? imageFile,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new { error = "Invalid request data", errors = errors });
                }

                if (imageFile != null)
                {
                    var config = new FileUploadConfig
                    {
                        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                        MaxSizeInMB = 5,
                        Folder = "service-categories/images",
                        SuccessMessage = "Service category image uploaded successfully",
                        EntityType = "service-category-image",
                    };

                    var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                        imageFile,
                        config,
                        Guid.Empty,
                        _logger,
                        cancellationToken
                    );

                    if (!uploadResult.Success)
                    {
                        return BadRequest(new { error = $"Image upload failed: {uploadResult.ErrorMessage}" });
                    }

                    request.ImageUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
                }

                var result = await _serviceMedicalService.CreateServiceCategoryAsync(request);
                return CreatedAtAction(nameof(GetServiceCategory), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service category with image");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Update service category with image upload (Admin only)
        /// </summary>
        [HttpPut("{id}/upload-image")]
        [Authorize(Policy = "Role:Admin")]
        public async Task<ActionResult<ServiceCategoryResponse>> UpdateServiceCategoryWithImage(
            Guid id,
            [FromForm] UpdateServiceCategoryRequest request,
            [FromForm] IFormFile? imageFile,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return BadRequest(new { error = "Invalid request data", errors = errors });
                }

                if (id != request.Id)
                {
                    return BadRequest(new { error = "ID mismatch" });
                }

                // Get current category to determine HospitalId if needed
                var current = await _serviceMedicalService.GetServiceCategoryByIdAsync(id);
                if (current == null)
                {
                    return NotFound(new { error = $"Service category with ID {id} not found" });
                }

                if (imageFile != null)
                {
                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(current.ImageUrl))
                    {
                        var deleteConfig = new FileDeletionConfig
                        {
                            FileUrl = current.ImageUrl,
                            ExpectedFolder = "service-categories",
                            SuccessMessage = "Old category image deleted",
                            EntityType = "service-category-image",
                        };
                        await _uploadOrchestrator.DeleteFileAsync(deleteConfig, Guid.Empty, _logger, cancellationToken);
                    }

                    var config = new FileUploadConfig
                    {
                        AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" },
                        MaxSizeInMB = 5,
                        Folder = "service-categories/images",
                        SuccessMessage = "Service category image uploaded successfully",
                        EntityType = "service-category-image",
                    };

                    var uploadResult = await _uploadOrchestrator.UploadFileAsync(
                        imageFile,
                        config,
                        Guid.Empty,
                        _logger,
                        cancellationToken
                    );

                    if (!uploadResult.Success)
                    {
                        return BadRequest(new { error = $"Image upload failed: {uploadResult.ErrorMessage}" });
                    }

                    request.ImageUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
                }

                var result = await _serviceMedicalService.UpdateServiceCategoryAsync(request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service category with image: {Id}", id);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get all active service categories
        /// </summary>
        /// <returns>List of active service categories</returns>
        [HttpGet("active")]
        public async Task<ActionResult<List<ServiceCategoryResponse>>> GetActiveServiceCategories()
        {
            try
            {
                var result = await _serviceMedicalService.GetActiveServiceCategoriesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active service categories");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        /// <summary>
        /// Get service category hierarchy (breadcrumb)
        /// </summary>
        /// <param name="categoryId">Service category ID</param>
        /// <returns>Hierarchy of service categories from root to specified category</returns>
        [HttpGet("{categoryId}/hierarchy")]
        public async Task<ActionResult<List<ServiceCategoryResponse>>> GetServiceCategoryHierarchy(Guid categoryId)
        {
            try
            {
                var result = await _serviceMedicalService.GetServiceCategoryHierarchyAsync(categoryId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service category hierarchy: {CategoryId}", categoryId);
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
            }
        }

        #endregion
    }
}
