using BookingCare.Services.ServiceMedical.Constants;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
using BookingCare.Services.ServiceMedical.Models.DTOs.Responses;
using BookingCare.Services.ServiceMedical.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.ServiceMedical.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Produces("application/json")]
    public class ServiceCategoriesController : ControllerBase
    {
        private readonly IServiceMedicalService _serviceMedicalService;
        private readonly ILogger<ServiceCategoriesController> _logger;

        public ServiceCategoriesController(IServiceMedicalService serviceMedicalService, ILogger<ServiceCategoriesController> logger)
        {
            _serviceMedicalService = serviceMedicalService;
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
                Service = "ServiceMedical - ServiceCategories",
                Timestamp = DateTime.UtcNow
            });
        }

        #endregion

        #region ServiceCategory CRUD Operations

        /// <summary>
        /// Create a new service category
        /// </summary>
        /// <param name="request">Service category creation request</param>
        /// <returns>Created service category</returns>
        [HttpPost]
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
        /// Update service category
        /// </summary>
        /// <param name="id">Service category ID</param>
        /// <param name="request">Service category update request</param>
        /// <returns>Updated service category</returns>
        [HttpPut("{id}")]
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
        /// Delete service category
        /// </summary>
        /// <param name="id">Service category ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
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
