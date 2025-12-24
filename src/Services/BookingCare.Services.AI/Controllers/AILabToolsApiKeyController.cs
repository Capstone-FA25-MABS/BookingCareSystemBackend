using BookingCare.Services.AI.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.AI.Controllers;

/// <summary>
/// Controller for managing AILabTools API keys
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/ailabtools-api-keys")]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
[Authorize] // Require authentication for security
public class AILabToolsApiKeyController : BaseApiController
{
    private readonly IAILabToolsApiKeyService _apiKeyService;
    private readonly ILogger<AILabToolsApiKeyController> _logger;

    public AILabToolsApiKeyController(
        IAILabToolsApiKeyService apiKeyService,
        ILogger<AILabToolsApiKeyController> logger)
    {
        _apiKeyService = apiKeyService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active API keys with their usage information, ordered by priority
    /// </summary>
    /// <returns>List of active API keys with priority order (1 = next to use)</returns>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(List<AILabToolsApiKeyInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActiveKeys()
    {
        try
        {
            var keys = await _apiKeyService.GetActiveKeysAsync();
            return Ok(keys);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active API keys");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get information about the next API key that will be used
    /// </summary>
    /// <returns>Information about the next key to use</returns>
    [HttpGet("next")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(AILabToolsApiKeyInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNextKey()
    {
        try
        {
            var nextKey = await _apiKeyService.GetNextKeyInfoAsync();
            if (nextKey == null)
            {
                return NotFound(new { message = "No active API keys available" });
            }

            return Ok(nextKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next API key");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Add a new API key to the database
    /// </summary>
    /// <param name="request">Request containing API key information</param>
    /// <returns>Created key ID</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddApiKey([FromBody] AddApiKeyRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ApiKey))
            {
                return BadRequest(new { error = "API key is required" });
            }

            var keyId = await _apiKeyService.AddApiKeyAsync(
                request.ApiKey,
                request.MaxUsageCount ?? 10,
                request.Notes);

            return CreatedAtAction(nameof(GetActiveKeys), new { id = keyId }, new { id = keyId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding API key");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Deactivate an API key
    /// </summary>
    /// <param name="id">API key ID</param>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateKey(Guid id)
    {
        try
        {
            await _apiKeyService.DeactivateKeyAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating API key {KeyId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for adding API key
/// </summary>
public class AddApiKeyRequest
{
    /// <summary>
    /// API key value
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Maximum usage count before deletion (default: 10)
    /// </summary>
    public int? MaxUsageCount { get; set; }

    /// <summary>
    /// Optional notes for this key
    /// </summary>
    public string? Notes { get; set; }
}

