using BookingCare.Services.Doctor.Models.DTOs;
using BookingCare.Services.Doctor.Services;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DoctorsController : BaseApiController
{
    private readonly IDoctorService _doctorService;
    private readonly ILogger<DoctorsController> _logger;

    public DoctorsController(IDoctorService doctorService, ILogger<DoctorsController> logger)
    {
        _doctorService = doctorService;
        _logger = logger;
    }

    #region Doctor Endpoints

    /// <summary>
    /// Get doctor by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDoctor(Guid id)
    {
        var doctor = await _doctorService.GetDoctorByIdAsync(id);
        if (doctor == null)
        {
            return NotFound($"Doctor with ID {id} not found");
        }

        return Success<DoctorResponse>(doctor, "Doctor retrieved successfully");
    }

    /// <summary>
    /// Get doctor by email
    /// </summary>
    [HttpGet("by-email/{email}")]
    public async Task<IActionResult> GetDoctorByEmail(string email)
    {
        var doctor = await _doctorService.GetDoctorByEmailAsync(email);
        if (doctor == null)
        {
            return NotFound($"Doctor with email '{email}' not found");
        }

        return Success<DoctorResponse>(doctor, "Doctor retrieved successfully");
    }

    /// <summary>
    /// Get doctor by account ID
    /// </summary>
    [HttpGet("by-account/{accountId}")]
    public async Task<IActionResult> GetDoctorByAccountId(Guid accountId)
    {
        var doctor = await _doctorService.GetDoctorByIdAsync(accountId);
        if (doctor == null)
        {
            return NotFound($"Doctor with account ID {accountId} not found");
        }

        return Success<DoctorResponse>(doctor, "Doctor retrieved successfully");
    }

    /// <summary>
    /// Get doctors with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDoctors([FromQuery] DoctorQueryRequest query)
    {
        var result = await _doctorService.GetDoctorsAsync(query);
        return Success<DoctorListResponse>(result, "Doctors retrieved successfully");
    }

    /// <summary>
    /// Filter doctors nâng cao theo nhiều tiêu chí (chuyên khoa, lịch trống, gender, số năm kinh nghiệm, giá, phòng khám, loại tư vấn, ngôn ngữ, đánh giá, địa chỉ, loại hình dịch vụ)
    /// </summary>
    [HttpPost("filter")]
    public async Task<IActionResult> FilterDoctors([FromBody] DoctorAdvancedFilterRequest filter)
    {
        var result = await _doctorService.FilterDoctorsAsync(filter);
        return Success<DoctorListResponse>(result, "Doctors filtered successfully");
    }

    /// <summary>
    /// Get doctors by clinic
    /// </summary>
    [HttpGet("clinic/{clinicId}")]
    public async Task<IActionResult> GetDoctorsByClinic(Guid clinicId)
    {
        var doctors = await _doctorService.GetDoctorsByClinicAsync(clinicId);
        return Success<List<DoctorResponse>>(doctors, $"Doctors for clinic {clinicId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by specialty
    /// </summary>
    [HttpGet("specialty/{specialtyId}")]
    public async Task<IActionResult> GetDoctorsBySpecialty(Guid specialtyId)
    {
        var doctors = await _doctorService.GetDoctorsBySpecialtyAsync(specialtyId);
        return Success<List<DoctorResponse>>(doctors, $"Doctors for specialty {specialtyId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by position
    /// </summary>
    [HttpGet("position/{positionId}")]
    public async Task<IActionResult> GetDoctorsByPosition(Guid positionId)
    {
        var doctors = await _doctorService.GetDoctorsByPositionAsync(positionId);
        return Success<List<DoctorResponse>>(doctors, $"Doctors for position {positionId} retrieved successfully");
    }

    /// <summary>
    /// Get active doctors
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveDoctors()
    {
        var doctors = await _doctorService.GetActiveDoctorsAsync();
        return Success<List<DoctorResponse>>(doctors, "Active doctors retrieved successfully");
    }

    /// <summary>
    /// Create a new doctor
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateDoctor([FromBody] CreateDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        try
        {
            var doctor = await _doctorService.CreateDoctorAsync(request);
            return Created(doctor, "Doctor created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor: {Message}", ex.Message);
            return StatusCode(500, new
            {
                success = false,
                message = ex.Message,
                errors = new[] { ex.GetType().Name },
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Update doctor information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDoctor(Guid id, [FromBody] UpdateDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id; // Ensure the ID in the request matches the route parameter
        var doctor = await _doctorService.UpdateDoctorAsync(request);
        return Success<DoctorResponse>(doctor, "Doctor updated successfully");
    }

    /// <summary>
    /// Delete doctor
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDoctor(Guid id)
    {
        var result = await _doctorService.DeleteDoctorAsync(id);
        if (!result)
        {
            return NotFound($"Doctor with ID {id} not found");
        }

        return Success<object?>(null, "Doctor deleted successfully");
    }

    /// <summary>
    /// Validate doctor existence
    /// </summary>
    [HttpGet("{id}/validate")]
    public async Task<IActionResult> ValidateDoctor(Guid id)
    {
        var exists = await _doctorService.DoctorExistsAsync(id);
        return Success<object>(new { exists }, "Doctor validation completed");
    }

    #endregion

    #region Doctor-Price Relationship Endpoints

    /// <summary>
    /// Get doctor's prices
    /// </summary>
    [HttpGet("{doctorId}/prices")]
    public async Task<IActionResult> GetDoctorPrices(Guid doctorId)
    {
        var prices = await _doctorService.GetDoctorPricesAsync(doctorId);
        return Success<List<PriceResponse>>(prices, $"Prices for doctor {doctorId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by price
    /// </summary>
    [HttpGet("prices/{priceId}/doctors")]
    public async Task<IActionResult> GetDoctorsByPrice(Guid priceId)
    {
        var doctors = await _doctorService.GetDoctorsByPriceAsync(priceId);
        return Success<List<DoctorResponse>>(doctors, $"Doctors for price {priceId} retrieved successfully");
    }

    /// <summary>
    /// Assign price to doctor
    /// </summary>
    [HttpPost("assign-price")]
    public async Task<IActionResult> AssignPriceToDoctor([FromBody] AssignPriceToDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var doctorPrice = await _doctorService.AssignPriceToDoctorAsync(request);
        return Created(doctorPrice, "Price assigned to doctor successfully");
    }

    /// <summary>
    /// Remove price from doctor
    /// </summary>
    [HttpDelete("{doctorId}/prices/{priceId}")]
    public async Task<IActionResult> RemovePriceFromDoctor(Guid doctorId, Guid priceId)
    {
        var result = await _doctorService.RemovePriceFromDoctorAsync(doctorId, priceId);
        if (!result)
        {
            return NotFound($"Price {priceId} not assigned to doctor {doctorId}");
        }

        return Success<object?>(null, "Price removed from doctor successfully");
    }

    #endregion

    #region Validation Endpoints

    /// <summary>
    /// Check if doctor email exists
    /// </summary>
    [HttpGet("validate/email/{email}")]
    public async Task<IActionResult> ValidateDoctorEmail(string email, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _doctorService.DoctorEmailExistsAsync(email, excludeId);
        return Success<object>(new { exists }, "Email validation completed");
    }

    /// <summary>
    /// Check if doctor account exists
    /// </summary>
    [HttpGet("validate/account/{accountId}")]
    public async Task<IActionResult> ValidateDoctorAccount(Guid accountId, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _doctorService.DoctorAccountExistsAsync(accountId, excludeId);
        return Success<object>(new { exists }, "Account validation completed");
    }

    /// <summary>
    /// Check if doctor-price relationship exists
    /// </summary>
    [HttpGet("validate/doctor-price")]
    public async Task<IActionResult> ValidateDoctorPrice([FromQuery] Guid doctorId, [FromQuery] Guid priceId)
    {
        var exists = await _doctorService.DoctorPriceExistsAsync(doctorId, priceId);
        return Success<object>(new { exists }, "Doctor-price relationship validation completed");
    }

    #endregion
}
