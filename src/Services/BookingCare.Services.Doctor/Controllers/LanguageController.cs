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
public class LanguageController : BaseApiController
{
    private readonly ILanguageService _languageService;

    public LanguageController(ILanguageService languageService)
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

    /// <summary>
    /// Tạo ngôn ngữ mới
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<LanguageResponse>> CreateLanguage([FromBody] CreateLanguageRequest request)
    {
        try
        {
            var language = await _languageService.CreateLanguageAsync(request);
            return CreatedAtAction(nameof(GetLanguageById), new { id = language.Id }, language);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy ngôn ngữ theo ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<LanguageResponse>> GetLanguageById(Guid id)
    {
        var language = await _languageService.GetLanguageByIdAsync(id);
        if (language == null)
        {
            return NotFound(new { message = "Language not found" });
        }
        return Ok(language);
    }

    /// <summary>
    /// Lấy ngôn ngữ theo tên
    /// </summary>
    [HttpGet("by-name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<LanguageResponse>> GetLanguageByName(string name)
    {
        var language = await _languageService.GetLanguageByNameAsync(name);
        if (language == null)
        {
            return NotFound(new { message = "Language not found" });
        }
        return Ok(language);
    }

    /// <summary>
    /// Lấy danh sách ngôn ngữ với phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<LanguageListResponse>> GetLanguages([FromQuery] LanguageQueryRequest query)
    {
        var result = await _languageService.GetLanguagesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy tất cả ngôn ngữ (không phân trang)
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<List<LanguageResponse>>> GetAllLanguages()
    {
        var languages = await _languageService.GetAllLanguagesAsync();
        return Ok(languages);
    }

    /// <summary>
    /// Cập nhật ngôn ngữ
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<LanguageResponse>> UpdateLanguage(Guid id, [FromBody] UpdateLanguageRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        try
        {
            var language = await _languageService.UpdateLanguageAsync(request);
            return Ok(language);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa ngôn ngữ
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult> DeleteLanguage(Guid id)
    {
        var result = await _languageService.DeleteLanguageAsync(id);
        if (!result)
        {
            return NotFound(new { message = "Language not found" });
        }
        return NoContent();
    }
}
