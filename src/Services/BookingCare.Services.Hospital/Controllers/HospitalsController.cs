using Microsoft.AspNetCore.Mvc;
using BookingCare.Services.Hospital.Services.Interfaces;
using BookingCare.Services.Hospital.Models.DTOs.Requests;
using BookingCare.Services.Hospital.Exceptions;
using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.Common.Controllers;

namespace BookingCare.Services.Hospital.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
public class HospitalsController : BaseApiController
{
    private readonly IHospitalService _hospitalService;
    private readonly ILogger<HospitalsController> _logger;

    public HospitalsController(IHospitalService hospitalService, ILogger<HospitalsController> logger)
    {
        _hospitalService = hospitalService;
        _logger = logger;
    }

    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { Status = "Healthy", Service = "Hospital", Timestamp = DateTime.UtcNow });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllHospitals([FromQuery] HospitalFilterRequest filter)
    {
        try
        {
            var result = await _hospitalService.GetFilteredAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospitals");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHospitalById(Guid id)
    {
        try
        {
            var hospital = await _hospitalService.GetByIdAsync(id);
            if (hospital == null)
            {
                return NotFound(new { Message = $"Hospital with ID {id} not found" });
            }
            return Ok(hospital);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospital with ID {HospitalId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("email/{email}")]
    public async Task<IActionResult> GetHospitalByEmail(string email)
    {
        try
        {
            var hospital = await _hospitalService.GetByEmailAsync(email);
            if (hospital == null)
            {
                return NotFound(new { Message = $"Hospital with email {email} not found" });
            }
            return Ok(hospital);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospital with email {Email}", email);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpGet("account")]
    public async Task<IActionResult> GetHospitalsByAccountId()
    {
        try
        {
            var accountId = JwtHelper.GetAccountIdFromClaimsOrThrow(HttpContext);
            var hospitals = await _hospitalService.GetByAccountIdAsync(accountId);
            if (hospitals == null)
            {
                return NotFound("Hospital profile not found");
            }
            return Success(hospitals, "Hospital retrieved successfully");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }

    }

    [HttpGet("specialty/{specialtyId}")]
    public async Task<IActionResult> GetHospitalsBySpecialty(Guid specialtyId)
    {
        try
        {
            var hospitals = await _hospitalService.GetBySpecialtyAsync(specialtyId);
            return Ok(hospitals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hospitals for specialty {SpecialtyId}", specialtyId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateHospital([FromBody] CreateHospitalRequest request)
    {
        try
        {
            var hospital = await _hospitalService.CreateAsync(request);
            return CreatedAtAction(nameof(GetHospitalById), new { id = hospital.Id }, hospital);
        }
        catch (HospitalAlreadyExistsException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (InvalidHospitalDataException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating hospital");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHospital(Guid id, [FromBody] UpdateHospitalRequest request)
    {
        try
        {
            var hospital = await _hospitalService.UpdateAsync(id, request);
            return Ok(hospital);
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (HospitalAlreadyExistsException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
        catch (InvalidHospitalDataException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating hospital with ID {HospitalId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteHospital(Guid id)
    {
        try
        {
            var result = await _hospitalService.DeleteAsync(id);
            if (result)
            {
                return NoContent();
            }
            return NotFound(new { Message = $"Hospital with ID {id} not found" });
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hospital with ID {HospitalId}", id);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpPost("{hospitalId}/specialties/{specialtyId}")]
    public async Task<IActionResult> AddSpecialtyToHospital(Guid hospitalId, Guid specialtyId)
    {
        try
        {
            var result = await _hospitalService.AddSpecialtyAsync(hospitalId, specialtyId);
            if (result)
            {
                return Ok(new { Message = "Specialty added successfully" });
            }
            return BadRequest(new { Message = "Failed to add specialty" });
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding specialty {SpecialtyId} to hospital {HospitalId}", specialtyId, hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    [HttpDelete("{hospitalId}/specialties/{specialtyId}")]
    public async Task<IActionResult> RemoveSpecialtyFromHospital(Guid hospitalId, Guid specialtyId)
    {
        try
        {
            var result = await _hospitalService.RemoveSpecialtyAsync(hospitalId, specialtyId);
            if (result)
            {
                return Ok(new { Message = "Specialty removed successfully" });
            }
            return BadRequest(new { Message = "Failed to remove specialty" });
        }
        catch (HospitalNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing specialty {SpecialtyId} from hospital {HospitalId}", specialtyId, hospitalId);
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }
}