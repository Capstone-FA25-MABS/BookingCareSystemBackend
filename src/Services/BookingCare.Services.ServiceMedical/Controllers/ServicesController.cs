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
    public class ServicesController : ControllerBase
    {
        private readonly IServiceMedicalService _serviceMedicalService;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(IServiceMedicalService serviceMedicalService, ILogger<ServicesController> logger)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                return StatusCode(500, new { error = StatusConstants.InternalServerError });
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
        /// Delete service
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
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="includeInactive">Include inactive services</param>
        /// <returns>List of services in the category with hospital information</returns>
        [HttpGet("category/{categoryId}/with-hospital")]
        public async Task<ActionResult<ServicesByCategoryWithHospitalResponse>> GetServicesByCategoryWithHospital(
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

        #endregion
    }
}
