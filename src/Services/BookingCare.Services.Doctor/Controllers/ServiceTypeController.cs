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
public class ServiceTypeController : BaseApiController
{
    private readonly IServiceTypeService _serviceTypeService;

    public ServiceTypeController(IServiceTypeService serviceTypeService)
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

    /// <summary>
    /// Tạo loại dịch vụ mới
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<ServiceTypeResponse>> CreateServiceType([FromBody] CreateServiceTypeRequest request)
    {
        try
        {
            var serviceType = await _serviceTypeService.CreateServiceTypeAsync(request);
            return CreatedAtAction(nameof(GetServiceTypeById), new { id = serviceType.Id }, serviceType);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy loại dịch vụ theo ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<ServiceTypeResponse>> GetServiceTypeById(Guid id)
    {
        var serviceType = await _serviceTypeService.GetServiceTypeByIdAsync(id);
        if (serviceType == null)
        {
            return NotFound(new { message = "Service type not found" });
        }
        return Ok(serviceType);
    }

    /// <summary>
    /// Lấy loại dịch vụ theo tên
    /// </summary>
    [HttpGet("by-name/{name}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<ServiceTypeResponse>> GetServiceTypeByName(string name)
    {
        var serviceType = await _serviceTypeService.GetServiceTypeByNameAsync(name);
        if (serviceType == null)
        {
            return NotFound(new { message = "Service type not found" });
        }
        return Ok(serviceType);
    }

    /// <summary>
    /// Lấy danh sách loại dịch vụ với phân trang và tìm kiếm
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<ServiceTypeListResponse>> GetServiceTypes([FromQuery] ServiceTypeQueryRequest query)
    {
        var result = await _serviceTypeService.GetServiceTypesAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Lấy tất cả loại dịch vụ (không phân trang)
    /// </summary>
    [HttpGet("all")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<List<ServiceTypeResponse>>> GetAllServiceTypes()
    {
        var serviceTypes = await _serviceTypeService.GetAllServiceTypesAsync();
        return Ok(serviceTypes);
    }

    /// <summary>
    /// Cập nhật loại dịch vụ
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult<ServiceTypeResponse>> UpdateServiceType(Guid id, [FromBody] UpdateServiceTypeRequest request)
    {
        if (id != request.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        try
        {
            var serviceType = await _serviceTypeService.UpdateServiceTypeAsync(request);
            return Ok(serviceType);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa loại dịch vụ
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<ActionResult> DeleteServiceType(Guid id)
    {
        var result = await _serviceTypeService.DeleteServiceTypeAsync(id);
        if (!result)
        {
            return NotFound(new { message = "Service type not found" });
        }
        return NoContent();
    }
}
