using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using BookingCare.Shared.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Hospital.Models.DTOs.Requests;

namespace BookingCare.Services.Hospital.Controllers;

/// <summary>
/// Controller for admin signature management
/// </summary>
[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
[Authorize(Policy = "Role:Admin")]
public class AdminSignaturesController : BaseApiController
{
    private readonly IAdminSignatureService _signatureService;

    public AdminSignaturesController(IAdminSignatureService signatureService)
    {
        _signatureService = signatureService;
    }

    /// <summary>
    /// Get admin signature by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var signature = await _signatureService.GetByIdAsync(id);
        if (signature == null)
        {
            return NotFound("Admin signature not found");
        }

        return Success(signature);
    }

    /// <summary>
    /// Get active signature for current admin
    /// </summary>
    [HttpGet("active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveSignature()
    {
        try
        {
            var adminId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            var signature = await _signatureService.GetActiveSignatureAsync(adminId.ToString());
            if (signature == null)
            {
                return NotFound("No active signature found for this admin");
            }

            return Success(signature);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Get all admin signatures
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetAll()
    {
        var signatures = await _signatureService.GetAllAsync();
        return Success(signatures);
    }

    /// <summary>
    /// Create a new admin signature
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateAdminSignatureRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var adminId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            var signature = await _signatureService.CreateAsync(adminId.ToString(), request);
            return Success(signature, "Admin signature created successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Update an existing admin signature
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid id, [FromForm] UpdateAdminSignatureRequestDto request)
    {
        try
        {
            var adminId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            var signature = await _signatureService.UpdateAsync(id, adminId.ToString(), request);
            return Success(signature, "Admin signature updated successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    /// <summary>
    /// Delete an admin signature
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var adminId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            var result = await _signatureService.DeleteAsync(id, adminId.ToString());
            if (!result)
            {
                return NotFound("Admin signature not found");
            }

            return Success<object?>(null, "Admin signature deleted successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
