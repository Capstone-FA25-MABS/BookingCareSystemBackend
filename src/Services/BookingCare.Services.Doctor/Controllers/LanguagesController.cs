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
public class LanguagesController : BaseApiController
{
    private readonly ILanguageService _languageService;

    public LanguagesController(ILanguageService languageService)
    {
        _languageService = languageService;
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
            Service = "Languages",
            Version = HttpContext.GetRequestedApiVersion()?.ToString() ?? ApiVersions.Default,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Language Endpoints

    /// <summary>
    /// Get language by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetLanguage(Guid id)
    {
        var language = await _languageService.GetLanguageByIdAsync(id);
        if (language == null)
        {
            return NotFound($"Language with ID {id} not found");
        }

        return Success<LanguageResponse>(language, "Language retrieved successfully");
    }

    /// <summary>
    /// Get language by name
    /// </summary>
    [HttpGet("by-name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetLanguageByName(string name)
    {
        var language = await _languageService.GetLanguageByNameAsync(name);
        if (language == null)
        {
            return NotFound($"Language with name '{name}' not found");
        }

        return Success<LanguageResponse>(language, "Language retrieved successfully");
    }

    /// <summary>
    /// Get all languages
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetLanguages([FromQuery] LanguageQueryRequest query)
    {
        var result = await _languageService.GetLanguagesAsync(query);
        return Success<LanguageListResponse>(result, "Languages retrieved successfully");
    }

    /// <summary>
    /// Get all languages (no pagination)
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllLanguages()
    {
        var languages = await _languageService.GetAllLanguagesAsync();
        return Success<List<LanguageResponse>>(languages, "All languages retrieved successfully");
    }

    /// <summary>
    /// Create a new language
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var language = await _languageService.CreateLanguageAsync(request);
        return Created(language, "Language created successfully");
    }

    /// <summary>
    /// Update language
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] UpdateLanguageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var language = await _languageService.UpdateLanguageAsync(request);
        return Success<LanguageResponse>(language, "Language updated successfully");
    }

    /// <summary>
    /// Delete language
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteLanguage(Guid id)
    {
        var result = await _languageService.DeleteLanguageAsync(id);
        if (!result)
        {
            return NotFound($"Language with ID {id} not found");
        }

        return Success<object?>(null, "Language deleted successfully");
    }

    /// <summary>
    /// Toggle language status (ACTIVE/INACTIVE)
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ToggleLanguageStatus(Guid id)
    {
        var result = await _languageService.ToggleLanguageStatusAsync(id);
        if (!result)
        {
            return NotFound($"Language with ID {id} not found");
        }

        return Success<object?>(null, "Language status toggled successfully");
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Check if language name exists
    /// </summary>
    [HttpGet("validate/name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateLanguageName(string name, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _languageService.LanguageNameExistsAsync(name, excludeId);
        return Success<object>(new { exists }, "Language name validation completed");
    }

    #endregion
}
