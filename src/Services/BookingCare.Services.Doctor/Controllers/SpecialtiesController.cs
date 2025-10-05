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
public class SpecialtiesController : BaseApiController
{
    private readonly ISpecialtyService _specialtyService;

    public SpecialtiesController(ISpecialtyService specialtyService)
    {
        _specialtyService = specialtyService;
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
            return NotFound($"Specialty with ID {id} not found");
        }

        return Success<SpecialtyResponse>(specialty, "Specialty retrieved successfully");
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
            return NotFound($"Specialty with name '{name}' not found");
        }

        return Success<SpecialtyResponse>(specialty, "Specialty retrieved successfully");
    }

    /// <summary>
    /// Get all specialties
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetSpecialties([FromQuery] SpecialtyQueryRequest query)
    {
        var result = await _specialtyService.GetSpecialtiesAsync(query);
        return Success<SpecialtyListResponse>(result, "Specialties retrieved successfully");
    }

    /// <summary>
    /// Get all specialties (no pagination)
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllSpecialties()
    {
        var specialties = await _specialtyService.GetAllSpecialtiesAsync();
        return Success<List<SpecialtyResponse>>(specialties, "All specialties retrieved successfully");
    }

    /// <summary>
    /// Get active specialties only
    /// </summary>
    [HttpGet("active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveSpecialties()
    {
        var specialties = await _specialtyService.GetActiveSpecialtiesAsync();
        return Success<List<SpecialtyResponse>>(specialties, "Active specialties retrieved successfully");
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
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var specialty = await _specialtyService.CreateSpecialtyAsync(request);
        return Created(specialty, "Specialty created successfully");
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
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var specialty = await _specialtyService.UpdateSpecialtyAsync(request);
        return Success<SpecialtyResponse>(specialty, "Specialty updated successfully");
    }

    /// <summary>
    /// Delete specialty
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteSpecialty(Guid id)
    {
        var result = await _specialtyService.DeleteSpecialtyAsync(id);
        if (!result)
        {
            return NotFound($"Specialty with ID {id} not found");
        }

        return Success<object?>(null, "Specialty deleted successfully");
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
            return NotFound($"Specialty with ID {id} not found");
        }

        return Success<object?>(null, "Specialty status toggled successfully");
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
        return Success<object>(new { exists }, "Specialty name validation completed");
    }

    #endregion
}
