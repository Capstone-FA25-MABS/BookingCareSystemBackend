using BookingCare.Services.Discount.Models.DTOs;
using BookingCare.Services.Discount.Services;
using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.Common.Versioning;
using Microsoft.AspNetCore.Mvc;
using DiscountRequestDto = BookingCare.Services.Discount.Models.DTOs;

namespace BookingCare.Services.Discount.Controllers;

[ApiController]
[Produces("application/json")]
[Route(ApiRouteTemplates.Versioned)]
[ApiVersion(ApiVersions.V1_0)]
public class DiscountsController : BaseApiController
{
    private readonly IDiscountService _discountService;
    private readonly ILogger<DiscountsController> _logger;

    public DiscountsController(IDiscountService discountService, ILogger<DiscountsController> logger)
    {
        _discountService = discountService;
        _logger = logger;
    }

    /// <summary>
    /// Get discount by ID
    /// </summary>
    [HttpGet("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDiscount(Guid id)
    {
        var discount = await _discountService.GetDiscountByIdAsync(id);
        if (discount == null)
        {
            return NotFound($"Discount with ID {id} not found");
        }

        return Success(discount, "Discount retrieved successfully");
    }

    /// <summary>
    /// Get discount by code
    /// </summary>
    [HttpGet("by-code/{code}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDiscountByCode(string code)
    {
        var discount = await _discountService.GetDiscountByCodeAsync(code);
        if (discount == null)
        {
            return NotFound($"Discount with code '{code}' not found");
        }

        return Success(discount, "Discount retrieved successfully");
    }

    /// <summary>
    /// Get discounts with filtering and pagination
    /// </summary>
    [HttpGet]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetDiscounts([FromQuery] DiscountQueryRequest query)
    {
        var result = await _discountService.GetDiscountsAsync(query);
        return Success(result, "Discounts retrieved successfully");
    }

    /// <summary>
    /// Get active discounts for a clinic
    /// </summary>
    [HttpGet("clinic/{clinicId}/active")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetActiveDiscountsByClinic(Guid clinicId)
    {
        var discounts = await _discountService.GetActiveDiscountsByClinicAsync(clinicId);
        return Success(discounts, $"Active discounts for clinic {clinicId} retrieved successfully");
    }

    /// <summary>
    /// Get applicable discounts for specific clinic/specialty/doctor
    /// </summary>
    [HttpGet("applicable")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> GetApplicableDiscounts(
        [FromQuery] Guid clinicId,
        [FromQuery] Guid? specialtyId = null,
        [FromQuery] Guid? doctorId = null)
    {
        var discounts = await _discountService.GetApplicableDiscountsAsync(clinicId, specialtyId, doctorId);
        return Success(discounts, "Applicable discounts retrieved successfully");
    }

    /// <summary>
    /// Create a new discount
    /// </summary>
    [HttpPost]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var discount = await _discountService.CreateDiscountAsync(request);
        return Created(discount, "Discount created successfully");
    }

    /// <summary>
    /// Update an existing discount
    /// </summary>
    [HttpPut("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateDiscount(Guid id, [FromBody] UpdateDiscountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var discount = await _discountService.UpdateDiscountAsync(id, request);
        return Success(discount, "Discount updated successfully");
    }

    /// <summary>
    /// Delete a discount
    /// </summary>
    [HttpDelete("{id}")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeleteDiscount(Guid id)
    {
        var result = await _discountService.DeleteDiscountAsync(id);
        if (!result)
        {
            return NotFound($"Discount with ID {id} not found");
        }

        return Success("Discount deleted successfully");
    }

    /// <summary>
    /// Validate a discount code
    /// </summary>
    [HttpPost("validate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ValidateDiscount([FromBody] ValidateDiscountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _discountService.ValidateDiscountAsync(request);
        return Success(result, result.IsValid ? "Discount is valid" : "Discount validation completed");
    }

    /// <summary>
    /// Use a discount code
    /// </summary>
    [HttpPost("use")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UseDiscount([FromBody] UseDiscountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var result = await _discountService.UseDiscountAsync(request);
        return Success(result, result.Success ? "Discount applied successfully" : "Discount usage completed");
    }

    /// <summary>
    /// Revert discount usage (for order cancellations)
    /// </summary>
    [HttpPost("revert")]
    public async Task<IActionResult> RevertDiscountUsage([FromBody] DiscountRequestDto.RevertDiscountUsageRequest request)
    {
        var result = await _discountService.RevertDiscountUsageAsync(request.Code, request.ClinicId);
        if (!result)
        {
            return BadRequest("Unable to revert discount usage");
        }

        return Success("Discount usage reverted successfully");
    }

    /// <summary>
    /// Activate a discount
    /// </summary>
    [HttpPatch("{id}/activate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> ActivateDiscount(Guid id)
    {
        var result = await _discountService.ActivateDiscountAsync(id);
        if (!result)
        {
            return NotFound($"Discount with ID {id} not found");
        }

        return Success("Discount activated successfully");
    }

    /// <summary>
    /// Deactivate a discount
    /// </summary>
    [HttpPatch("{id}/deactivate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> DeactivateDiscount(Guid id)
    {
        var result = await _discountService.DeactivateDiscountAsync(id);
        if (!result)
        {
            return NotFound($"Discount with ID {id} not found");
        }

        return Success("Discount deactivated successfully");
    }

    /// <summary>
    /// Update expired discounts (admin operation)
    /// </summary>
    [HttpPost("update-expired")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> UpdateExpiredDiscounts()
    {
        var count = await _discountService.UpdateExpiredDiscountsAsync();
        return Success(new { UpdatedCount = count }, $"Updated {count} expired discounts");
    }

    /// <summary>
    /// Calculate discount amount for a given code and amount
    /// </summary>
    [HttpPost("calculate")]
    [MapToApiVersion(ApiVersions.V1_0)]
    public async Task<IActionResult> CalculateDiscountAmount([FromBody] DiscountRequestDto.CalculateDiscountRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var discountAmount = await _discountService.CalculateDiscountAmountAsync(
            request.Code,
            request.OriginalAmount,
            request.ClinicId,
            request.SpecialtyId,
            request.DoctorId);

        var finalAmount = request.OriginalAmount - discountAmount;

        var result = new
        {
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            OriginalAmount = request.OriginalAmount
        };

        return Success(result, "Discount amount calculated successfully");
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        var healthData = new { Status = "Healthy", Service = "Discount", Timestamp = DateTime.UtcNow };
        return Success(healthData, "Discount service is healthy");
    }
}

// Additional DTOs for specific endpoints
public class RevertDiscountUsageRequest
{
    public string Code { get; set; } = string.Empty;
    public long ClinicId { get; set; }
}

public class CalculateDiscountRequest
{
    public string Code { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public long ClinicId { get; set; }
    public long? SpecialtyId { get; set; }
    public long? DoctorId { get; set; }
}