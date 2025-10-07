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
public class PositionsController : BaseApiController
{
    private readonly IPositionService _positionService;

    public PositionsController(IPositionService positionService)
    {
        _positionService = positionService;
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
            Service = "Positions",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Position Endpoints

    /// <summary>
    /// Get position by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPosition(Guid id)
    {
        var position = await _positionService.GetPositionByIdAsync(id);
        if (position == null)
        {
            return NotFound($"Position with ID {id} not found");
        }

        return Success<PositionResponse>(position, "Position retrieved successfully");
    }

    /// <summary>
    /// Get position by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPositionByName(string name)
    {
        var position = await _positionService.GetPositionByNameAsync(name);
        if (position == null)
        {
            return NotFound($"Position with name '{name}' not found");
        }

        return Success<PositionResponse>(position, "Position retrieved successfully");
    }

    /// <summary>
    /// Get all positions
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetPositions([FromQuery] PositionQueryRequest query)
    {
        var result = await _positionService.GetPositionsAsync(query);
        return Success<PositionListResponse>(result, "Positions retrieved successfully");
    }

    /// <summary>
    /// Get all positions (no pagination)
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllPositions()
    {
        var positions = await _positionService.GetAllPositionsAsync();
        return Success<List<PositionResponse>>(positions, "All positions retrieved successfully");
    }

    /// <summary>
    /// Create a new position
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var position = await _positionService.CreatePositionAsync(request);
        return Created(position, "Position created successfully");
    }

    /// <summary>
    /// Update position
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdatePosition(Guid id, [FromBody] UpdatePositionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var position = await _positionService.UpdatePositionAsync(request);
        return Success<PositionResponse>(position, "Position updated successfully");
    }

    /// <summary>
    /// Delete position
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeletePosition(Guid id)
    {
        var result = await _positionService.DeletePositionAsync(id);
        if (!result)
        {
            return NotFound($"Position with ID {id} not found");
        }

        return Success<object?>(null, "Position deleted successfully");
    }

    /// <summary>
    /// Toggle position status (ACTIVE/INACTIVE)
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> TogglePositionStatus(Guid id)
    {
        var result = await _positionService.TogglePositionStatusAsync(id);
        if (!result)
        {
            return NotFound($"Position with ID {id} not found");
        }

        return Success<object?>(null, "Position status toggled successfully");
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Check if position name exists
    /// </summary>
    [HttpGet("validate/name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidatePositionName(string name, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _positionService.PositionNameExistsAsync(name, excludeId);
        return Success<object>(new { exists }, "Position name validation completed");
    }

    #endregion
}
