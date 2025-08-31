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

        return Success(doctor, "Doctor retrieved successfully");
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

        return Success(doctor, "Doctor retrieved successfully");
    }

    /// <summary>
    /// Get doctor by account ID
    /// </summary>
    [HttpGet("by-account/{accountId}")]
    public async Task<IActionResult> GetDoctorByAccountId(Guid accountId)
    {
        var doctor = await _doctorService.GetDoctorByAccountIdAsync(accountId);
        if (doctor == null)
        {
            return NotFound($"Doctor with account ID {accountId} not found");
        }

        return Success(doctor, "Doctor retrieved successfully");
    }

    /// <summary>
    /// Get doctors with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDoctors([FromQuery] DoctorQueryRequest query)
    {
        var result = await _doctorService.GetDoctorsAsync(query);
        return Success(result, "Doctors retrieved successfully");
    }

    /// <summary>
    /// Get doctors by clinic
    /// </summary>
    [HttpGet("clinic/{clinicId}")]
    public async Task<IActionResult> GetDoctorsByClinic(Guid clinicId)
    {
        var doctors = await _doctorService.GetDoctorsByClinicAsync(clinicId);
        return Success(doctors, $"Doctors for clinic {clinicId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by specialty
    /// </summary>
    [HttpGet("specialty/{specialtyId}")]
    public async Task<IActionResult> GetDoctorsBySpecialty(Guid specialtyId)
    {
        var doctors = await _doctorService.GetDoctorsBySpecialtyAsync(specialtyId);
        return Success(doctors, $"Doctors for specialty {specialtyId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by position
    /// </summary>
    [HttpGet("position/{positionId}")]
    public async Task<IActionResult> GetDoctorsByPosition(Guid positionId)
    {
        var doctors = await _doctorService.GetDoctorsByPositionAsync(positionId);
        return Success(doctors, $"Doctors for position {positionId} retrieved successfully");
    }

    /// <summary>
    /// Get active doctors
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveDoctors()
    {
        var doctors = await _doctorService.GetActiveDoctorsAsync();
        return Success(doctors, "Active doctors retrieved successfully");
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

        var doctor = await _doctorService.CreateDoctorAsync(request);
        return Created(doctor, "Doctor created successfully");
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
        return Success(doctor, "Doctor updated successfully");
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

        return Success(null, "Doctor deleted successfully");
    }

    /// <summary>
    /// Validate doctor existence
    /// </summary>
    [HttpGet("{id}/validate")]
    public async Task<IActionResult> ValidateDoctor(Guid id)
    {
        var exists = await _doctorService.DoctorExistsAsync(id);
        return Success(new { exists }, "Doctor validation completed");
    }

    #endregion

    #region Position Endpoints

    /// <summary>
    /// Get position by ID
    /// </summary>
    [HttpGet("positions/{id}")]
    public async Task<IActionResult> GetPosition(Guid id)
    {
        var position = await _doctorService.GetPositionByIdAsync(id);
        if (position == null)
        {
            return NotFound($"Position with ID {id} not found");
        }

        return Success(position, "Position retrieved successfully");
    }

    /// <summary>
    /// Get position by name
    /// </summary>
    [HttpGet("positions/by-name/{name}")]
    public async Task<IActionResult> GetPositionByName(string name)
    {
        var position = await _doctorService.GetPositionByNameAsync(name);
        if (position == null)
        {
            return NotFound($"Position with name '{name}' not found");
        }

        return Success(position, "Position retrieved successfully");
    }

    /// <summary>
    /// Get all positions
    /// </summary>
    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions([FromQuery] PositionQueryRequest query)
    {
        var result = await _doctorService.GetPositionsAsync(query);
        return Success(result, "Positions retrieved successfully");
    }

    /// <summary>
    /// Get all positions (no pagination)
    /// </summary>
    [HttpGet("positions/all")]
    public async Task<IActionResult> GetAllPositions()
    {
        var positions = await _doctorService.GetAllPositionsAsync();
        return Success(positions, "All positions retrieved successfully");
    }

    /// <summary>
    /// Create a new position
    /// </summary>
    [HttpPost("positions")]
    public async Task<IActionResult> CreatePosition([FromBody] CreatePositionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var position = await _doctorService.CreatePositionAsync(request);
        return Created(position, "Position created successfully");
    }

    /// <summary>
    /// Update position
    /// </summary>
    [HttpPut("positions/{id}")]
    public async Task<IActionResult> UpdatePosition(Guid id, [FromBody] UpdatePositionRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        request.Id = id;
        var position = await _doctorService.UpdatePositionAsync(request);
        return Success(position, "Position updated successfully");
    }

    /// <summary>
    /// Delete position
    /// </summary>
    [HttpDelete("positions/{id}")]
    public async Task<IActionResult> DeletePosition(Guid id)
    {
        var result = await _doctorService.DeletePositionAsync(id);
        if (!result)
        {
            return NotFound($"Position with ID {id} not found");
        }

        return Success(null, "Position deleted successfully");
    }

    #endregion

    #region Price Endpoints

    /// <summary>
    /// Get price by ID
    /// </summary>
    [HttpGet("prices/{id}")]
    public async Task<IActionResult> GetPrice(Guid id)
    {
        var price = await _doctorService.GetPriceByIdAsync(id);
        if (price == null)
        {
            return NotFound($"Price with ID {id} not found");
        }

        return Success(price, "Price retrieved successfully");
    }

    /// <summary>
    /// Get all prices
    /// </summary>
    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices([FromQuery] PriceQueryRequest query)
    {
        var result = await _doctorService.GetPricesAsync(query);
        return Success(result, "Prices retrieved successfully");
    }

    /// <summary>
    /// Get all prices (no pagination)
    /// </summary>
    [HttpGet("prices/all")]
    public async Task<IActionResult> GetAllPrices()
    {
        var prices = await _doctorService.GetAllPricesAsync();
        return Success(prices, "All prices retrieved successfully");
    }

    /// <summary>
    /// Create a new price
    /// </summary>
    [HttpPost("prices")]
    public async Task<IActionResult> CreatePrice([FromBody] CreatePriceRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid request data", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList());
        }

        var price = await _doctorService.CreatePriceAsync(request);
        return Created(price, "Price created successfully");
    }

    /// <summary>
    /// Update price
    /// </summary>
    [HttpPut("prices/{id}")]
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
        var price = await _doctorService.UpdatePriceAsync(request);
        return Success(price, "Price updated successfully");
    }

    /// <summary>
    /// Delete price
    /// </summary>
    [HttpDelete("prices/{id}")]
    public async Task<IActionResult> DeletePrice(Guid id)
    {
        var result = await _doctorService.DeletePriceAsync(id);
        if (!result)
        {
            return NotFound($"Price with ID {id} not found");
        }

        return Success(null, "Price deleted successfully");
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
        return Success(prices, $"Prices for doctor {doctorId} retrieved successfully");
    }

    /// <summary>
    /// Get doctors by price
    /// </summary>
    [HttpGet("prices/{priceId}/doctors")]
    public async Task<IActionResult> GetDoctorsByPrice(Guid priceId)
    {
        var doctors = await _doctorService.GetDoctorsByPriceAsync(priceId);
        return Success(doctors, $"Doctors for price {priceId} retrieved successfully");
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

        return Success(null, "Price removed from doctor successfully");
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
        return Success(new { exists }, "Email validation completed");
    }

    /// <summary>
    /// Check if doctor account exists
    /// </summary>
    [HttpGet("validate/account/{accountId}")]
    public async Task<IActionResult> ValidateDoctorAccount(Guid accountId, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _doctorService.DoctorAccountExistsAsync(accountId, excludeId);
        return Success(new { exists }, "Account validation completed");
    }

    /// <summary>
    /// Check if position name exists
    /// </summary>
    [HttpGet("positions/validate/name/{name}")]
    public async Task<IActionResult> ValidatePositionName(string name, [FromQuery] Guid? excludeId = null)
    {
        var exists = await _doctorService.PositionNameExistsAsync(name, excludeId);
        return Success(new { exists }, "Position name validation completed");
    }

    /// <summary>
    /// Check if doctor-price relationship exists
    /// </summary>
    [HttpGet("validate/doctor-price")]
    public async Task<IActionResult> ValidateDoctorPrice([FromQuery] Guid doctorId, [FromQuery] Guid priceId)
    {
        var exists = await _doctorService.DoctorPriceExistsAsync(doctorId, priceId);
        return Success(new { exists }, "Doctor-price relationship validation completed");
    }

    #endregion
}