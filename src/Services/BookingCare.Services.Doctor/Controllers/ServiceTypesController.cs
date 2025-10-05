using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class ServiceTypesController : BaseApiController
{
    private readonly IServiceTypeService _serviceTypeService;

    public ServiceTypesController(IServiceTypeService serviceTypeService)
    {
        _serviceTypeService = serviceTypeService;
    }

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
            return NotFound($"Service type with ID {id} not found");
        }

        return Success<ServiceTypeResponse>(serviceType, "Service type retrieved successfully");
    }

    /// <summary>
    /// Get service type by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetServiceTypeByName(string name)
    {
        var serviceType = await _serviceTypeService.GetServiceTypeByNameAsync(name);
        if (serviceType == null)
        {
            return NotFound($"Service type with name '{name}' not found");
        }

        return Success<ServiceTypeResponse>(serviceType, "Service type retrieved successfully");
    }

    /// <summary>
    /// Get all service types
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetServiceTypes([FromQuery] ServiceTypeQueryRequest query)
    {
        var result = await _serviceTypeService.GetServiceTypesAsync(query);
        return Success<ServiceTypeListResponse>(result, "Service types retrieved successfully");
    }

    /// <summary>
    /// Get all service types (no pagination)
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllServiceTypes()
    {
        var serviceTypes = await _serviceTypeService.GetAllServiceTypesAsync();
        return Success<List<ServiceTypeResponse>>(serviceTypes, "All service types retrieved successfully");
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
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var serviceType = await _serviceTypeService.CreateServiceTypeAsync(request);
        return Created(serviceType, "Service type created successfully");
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
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var serviceType = await _serviceTypeService.UpdateServiceTypeAsync(request);
        return Success<ServiceTypeResponse>(serviceType, "Service type updated successfully");
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
            return NotFound($"Service type with ID {id} not found");
        }

        return Success<object?>(null, "Service type deleted successfully");
    }

    /// <summary>
    /// Toggle service type status (ACTIVE/INACTIVE)
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ToggleDoctorServiceTypeStatus(Guid id)
    {
        var result = await _serviceTypeService.ToggleDoctorServiceTypeStatusAsync(id);
        if (!result)
        {
            return NotFound($"Service type with ID {id} not found");
        }

        return Success<object?>(null, "Service type status toggled successfully");
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Check if service type name exists
    /// </summary>
    [HttpGet("validate/name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateServiceTypeName(string name, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _serviceTypeService.ServiceTypeNameExistsAsync(name, excludeId);
        return Success<object>(new { exists }, "Service type name validation completed");
    }

    #endregion
}
