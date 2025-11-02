using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Models.DTOs.Responses;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Hospital.Controllers;

[ApiController]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
[Produces("application/json")]
public class HospitalRegistrationsController : BaseApiController
{
    private readonly IHospitalRegistrationService _registrationService;

    public HospitalRegistrationsController(IHospitalRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    /// <summary>
    /// Submit a new hospital partnership registration (Public endpoint)
    /// </summary>
    /// <param name="request">Registration request data</param>
    /// <returns>Created registration details</returns>
    [HttpPost("submit")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HospitalRegistrationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitRegistration([FromForm] CreateHospitalRegistrationRequestDto request)
    {
        var result = await _registrationService.CreateRegistrationAsync(request);
        return Created(result, "Đơn đăng ký hợp tác đã được gửi thành công. Chúng tôi sẽ liên hệ lại trong thời gian sớm nhất!");
    }

    /// <summary>
    /// Get registration by ID (Admin only)
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <returns>Registration details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(HospitalRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRegistrationById(Guid id)
    {
        var registration = await _registrationService.GetRegistrationByIdAsync(id);
        return Success(registration);
    }

    /// <summary>
    /// Get all registrations with filtering and pagination (Admin only)
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>Paginated list of registrations</returns>
    [HttpGet]
    //[Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(HospitalRegistrationListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllRegistrations([FromQuery] HospitalRegistrationFilterRequestDto filter)
    {
        var result = await _registrationService.GetAllRegistrationsAsync(filter);
        return Success(result);
    }

    /// <summary>
    /// Update registration status (Admin only)
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <param name="request">Status update request</param>
    /// <returns>Updated registration</returns>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(HospitalRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRegistrationStatus(
        Guid id,
        [FromForm] UpdateRegistrationStatusRequestDto request)
    {
        var result = await _registrationService.UpdateRegistrationStatusAsync(id, request);
        return Success(result, "Cập nhật trạng thái đơn đăng ký thành công");
    }

    /// <summary>
    /// Delete registration (Admin only)
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRegistration(Guid id)
    {
        await _registrationService.DeleteRegistrationAsync(id);
        return Success("Xóa đơn đăng ký thành công");
    }

    /// <summary>
    /// Approve hospital registration and trigger account creation (Admin only)
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <param name="request">Approval request with contract file</param>
    /// <returns>Updated registration</returns>
    [HttpPost("{id:guid}/approve")]
    //[Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(HospitalRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRegistration(
        Guid id,
        [FromForm] ApproveRegistrationRequestDto request)
    {
        var result = await _registrationService.ApproveRegistrationAsync(id, request);
        return Success(result, "Đơn đăng ký đã được phê duyệt. Hệ thống đang tạo tài khoản cho bệnh viện...");
    }

    /// <summary>
    /// Reject hospital registration (Admin only)
    /// </summary>
    /// <param name="id">Registration ID</param>
    /// <param name="request">Rejection request with reason</param>
    /// <returns>Updated registration</returns>
    [HttpPost("{id:guid}/reject")]
    //[Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(HospitalRegistrationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRegistration(
        Guid id,
        [FromBody] RejectRegistrationRequestDto request)
    {
        var result = await _registrationService.RejectRegistrationAsync(id, request);
        return Success(result, "Đơn đăng ký đã bị từ chối");
    }
}

