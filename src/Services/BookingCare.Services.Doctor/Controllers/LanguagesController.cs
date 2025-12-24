using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.DTOs.Responses;
using BookingCare.Services.Doctor.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
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
            return NotFound($"Không tìm thấy ngôn ngữ với ID {id}");
        }

        return Success<LanguageResponse>(language, "Lấy thông tin ngôn ngữ thành công");
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
            return NotFound($"Không tìm thấy ngôn ngữ với tên '{name}'");
        }

        return Success<LanguageResponse>(language, "Lấy thông tin ngôn ngữ thành công");
    }

    /// <summary>
    /// Get all languages
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetLanguages([FromQuery] LanguageQueryRequest query)
    {
        var result = await _languageService.GetLanguagesAsync(query);
        return Success<LanguageListResponse>(result, "Lấy danh sách ngôn ngữ thành công");
    }

    /// <summary>
    /// Get all languages (no pagination) - Optimized for performance
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAllLanguages()
    {
        var languages = await _languageService.GetActiveLanguagesSimpleAsync();
        return Success<List<LanguageSimpleResponse>>(languages, "Lấy tất cả ngôn ngữ hoạt động thành công");
    }

    /// <summary>
    /// Create a new language
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> CreateLanguage([FromBody] CreateLanguageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu yêu cầu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var language = await _languageService.CreateLanguageAsync(request);
        return Created(language, "Tạo ngôn ngữ thành công");
    }

    /// <summary>
    /// Update language
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> UpdateLanguage(Guid id, [FromBody] UpdateLanguageRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu yêu cầu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var language = await _languageService.UpdateLanguageAsync(request);
        return Success<LanguageResponse>(language, "Cập nhật ngôn ngữ thành công");
    }

    /// <summary>
    /// Delete language
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> DeleteLanguage(Guid id)
    {
        var result = await _languageService.DeleteLanguageAsync(id);
        if (!result)
        {
            return NotFound($"Không tìm thấy ngôn ngữ với ID {id}");
        }

        return Success<object?>(null, "Xóa ngôn ngữ thành công");
    }

    /// <summary>
    /// Toggle language status (ACTIVE/INACTIVE)
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Authorize(Policy = "Role:Admin")]
    public async Task<IActionResult> ToggleLanguageStatus(Guid id)
    {
        var result = await _languageService.ToggleLanguageStatusAsync(id);
        if (!result)
        {
            return NotFound($"Không tìm thấy ngôn ngữ với ID {id}");
        }

        return Success<object?>(null, "Thay đổi trạng thái ngôn ngữ thành công");
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
        return Success<object>(new { exists }, "Kiểm tra tên ngôn ngữ hoàn tất");
    }

    #endregion
}
