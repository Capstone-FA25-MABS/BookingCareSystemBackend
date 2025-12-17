using BookingCare.Services.Content.Exceptions;
using BookingCare.Services.Content.Models.DTOs;
using BookingCare.Services.Content.Services;
using BookingCare.Shared.Common.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Content.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hospital-faqs")]
public class HospitalFaqsController : ControllerBase
{
    private readonly IHospitalFaqService _faqService;
    private readonly ILogger<HospitalFaqsController> _logger;

    public HospitalFaqsController(
        IHospitalFaqService faqService,
        ILogger<HospitalFaqsController> logger)
    {
        _faqService = faqService;
        _logger = logger;
    }

    /// <summary>
    /// Get FAQs with optional filtering.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HospitalFaqListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFaqs(
        [FromQuery] HospitalFaqFilterRequest filter,
        CancellationToken cancellationToken = default)
    {
        var faqs = await _faqService.GetFaqsAsync(filter, cancellationToken);
        return Ok(faqs);
    }

    /// <summary>
    /// Get all FAQs for a specific hospital.
    /// </summary>
    [HttpGet("hospital/{hospitalId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<HospitalFaqResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFaqsByHospitalId(
        Guid hospitalId,
        CancellationToken cancellationToken = default)
    {
        var faqs = await _faqService.GetFaqsByHospitalIdAsync(hospitalId, cancellationToken);
        return Ok(faqs);
    }

    /// <summary>
    /// Get a specific FAQ by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HospitalFaqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFaq(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var faq = await _faqService.GetByIdAsync(id, cancellationToken);
        return faq == null ? NotFound() : Ok(faq);
    }

    /// <summary>
    /// Create a new FAQ (Staff & Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Role:Admin,Staff")]
    [ProducesResponseType(typeof(HospitalFaqResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFaq(
        [FromBody] CreateHospitalFaqRequest request,
        CancellationToken cancellationToken = default)
    {
        var accountId = JwtHelper.GetAccountIdFromClaims(HttpContext);
        if (!accountId.HasValue)
        {
            return Forbid();
        }

        var faq = await _faqService.CreateFaqAsync(request, accountId.Value, cancellationToken);
        return CreatedAtAction(nameof(GetFaq), new { id = faq.Id }, faq);
    }

    /// <summary>
    /// Update an existing FAQ (Staff & Admin only).
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Role:Admin,Staff")]
    [ProducesResponseType(typeof(HospitalFaqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFaq(
        Guid id,
        [FromBody] UpdateHospitalFaqRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var faq = await _faqService.UpdateFaqAsync(id, request, cancellationToken);
            return Ok(faq);
        }
        catch (HospitalFaqNotFoundException)
        {
            return NotFound();
        }
    }

    /// <summary>
    /// Delete an FAQ (Staff & Admin only).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Role:Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFaq(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _faqService.DeleteFaqAsync(id, cancellationToken);
            return NoContent();
        }
        catch (HospitalFaqNotFoundException)
        {
            return NotFound();
        }
    }
}


