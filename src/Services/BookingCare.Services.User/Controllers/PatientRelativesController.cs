using BookingCare.Services.User.Models.DTOs;
using BookingCare.Services.User.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.User.Controllers;

/// <summary>
/// Controller for managing patient relatives (family members)
/// </summary>
[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Authorize(Policy = "Role:Patient")]
public class PatientRelativesController : BaseApiController
{
    private readonly IPatientRelativeService _relativeService;
    private readonly IUserService _userService;

    public PatientRelativesController(
        IPatientRelativeService relativeService,
        IUserService userService)
    {
        _relativeService = relativeService;
        _userService = userService;
    }

    /// <summary>
    /// Get all relatives for the current user
    /// </summary>
    /// <returns>List of patient relatives</returns>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetMyRelatives()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await _relativeService.GetMyRelativesAsync(userId);
            return Success(result, "Lấy danh sách người thân thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Get all relatives for dropdown/selection (lightweight)
    /// </summary>
    /// <returns>List of patient relatives with basic info</returns>
    [HttpGet("basic")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetMyRelativesBasic()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await _relativeService.GetMyRelativesBasicAsync(userId);
            return Success(result, "Lấy danh sách người thân thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Get a specific relative by ID
    /// </summary>
    /// <param name="id">Relative ID</param>
    /// <returns>Patient relative details</returns>
    [HttpGet("{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetRelativeById(Guid id)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await _relativeService.GetRelativeByIdAsync(id, userId);
            if (result == null)
            {
                return NotFound("Không tìm thấy người thân");
            }
            return Success(result, "Lấy thông tin người thân thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Create a new relative
    /// </summary>
    /// <param name="request">Relative creation request</param>
    /// <returns>Created relative</returns>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateRelative([FromBody] CreatePatientRelativeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await _relativeService.CreateRelativeAsync(userId, request);
            return Success(result, "Thêm người thân thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing relative
    /// </summary>
    /// <param name="id">Relative ID</param>
    /// <param name="request">Relative update request</param>
    /// <returns>Updated relative</returns>
    [HttpPut("{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateRelative(Guid id, [FromBody] UpdatePatientRelativeRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Dữ liệu không hợp lệ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await _relativeService.UpdateRelativeAsync(id, userId, request);
            return Success(result, "Cập nhật thông tin người thân thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Delete a relative
    /// </summary>
    /// <param name="id">Relative ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteRelative(Guid id)
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var result = await _relativeService.DeleteRelativeAsync(id, userId);
            return Success(result, "Xóa người thân thành công");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    #region Private Helpers

    /// <summary>
    /// Get current user ID from accountId in JWT token
    /// </summary>
    private async Task<Guid> GetCurrentUserIdAsync()
    {
        var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
        var user = await _userService.GetByAccountIdAsync(accountId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng");
        }
        return user.Id;
    }

    #endregion
}
