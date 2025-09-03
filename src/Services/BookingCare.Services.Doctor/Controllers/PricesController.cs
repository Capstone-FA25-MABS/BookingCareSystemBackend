using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Services;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PricesController : BaseApiController
{
    private readonly IPriceService _priceService;
    private readonly ILogger<PricesController> _logger;

    public PricesController(IPriceService priceService, ILogger<PricesController> logger)
    {
        _priceService = priceService;
        _logger = logger;
    }

    #region Price Endpoints

    /// <summary>
    /// Get price by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPrice(Guid id)
    {
        var price = await _priceService.GetPriceByIdAsync(id);
        if (price == null)
        {
            return NotFound($"Price with ID {id} not found");
        }

        return Success<PriceResponse>(price, "Price retrieved successfully");
    }

    /// <summary>
    /// Get all prices
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPrices([FromQuery] PriceQueryRequest query)
    {
        var result = await _priceService.GetPricesAsync(query);
        return Success<PriceListResponse>(result, "Prices retrieved successfully");
    }

    /// <summary>
    /// Get all prices (no pagination)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllPrices()
    {
        var prices = await _priceService.GetAllPricesAsync();
        return Success<List<PriceResponse>>(prices, "All prices retrieved successfully");
    }

    /// <summary>
    /// Create a new price
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreatePrice([FromBody] CreatePriceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var price = await _priceService.CreatePriceAsync(request);
        return Created(price, "Price created successfully");
    }

    /// <summary>
    /// Update price
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] UpdatePriceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var price = await _priceService.UpdatePriceAsync(request);
        return Success<PriceResponse>(price, "Price updated successfully");
    }

    /// <summary>
    /// Delete price
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePrice(Guid id)
    {
        var result = await _priceService.DeletePriceAsync(id);
        if (!result)
        {
            return NotFound($"Price with ID {id} not found");
        }

        return Success<object?>(null, "Price deleted successfully");
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Check if price exists
    /// </summary>
    [HttpGet("{id}/validate")]
    public async Task<IActionResult> ValidatePrice(Guid id)
    {
        var exists = await _priceService.PriceExistsAsync(id);
        return Success<object>(new { exists }, "Price validation completed");
    }

    #endregion
}
