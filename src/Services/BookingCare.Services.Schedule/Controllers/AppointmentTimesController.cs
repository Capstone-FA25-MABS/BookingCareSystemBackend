using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Services;
using BookingCare.Shared.Common.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Schedule.Controllers;

/// <summary>
/// Controller for managing appointment times
/// </summary>
[Route("api/v{version:apiVersion}/appointment-times")]
public class AppointmentTimesController : BaseApiController
{
    private readonly IScheduleService _scheduleService;

    public AppointmentTimesController(IScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    /// <summary>
    /// Get all appointment times
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllAppointmentTimes()
    {
        var appointmentTimes = await _scheduleService.GetAllAppointmentTimesAsync();
        return Success(appointmentTimes, "Appointment times retrieved successfully");
    }

    /// <summary>
    /// Get appointment time by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointmentTimeById(long id)
    {
        var appointmentTime = await _scheduleService.GetAppointmentTimeByIdAsync(id);
        if (appointmentTime == null)
        {
            return NotFound("Appointment time not found");
        }
        return Success(appointmentTime, "Appointment time retrieved successfully");
    }

    /// <summary>
    /// Create a new appointment time
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateAppointmentTime([FromBody] CreateAppointmentTimeRequest request)
    {
        var appointmentTime = await _scheduleService.CreateAppointmentTimeAsync(request);
        return Success(appointmentTime, "Appointment time created successfully");
    }
}