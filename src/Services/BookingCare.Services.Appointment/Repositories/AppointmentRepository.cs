using BookingCare.Services.Appointment.Data;
using BookingCare.Services.Appointment.Models.DTOs;
using BookingCare.Services.Appointment.Models.Entities;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Appointment.Enums;
using BookingCare.Services.Appointment.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Appointment.Repositories;

/// <summary>
/// Repository implementation for Appointment service operations
/// </summary>
public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppointmentDbContext _context;
    private readonly ILogger<AppointmentRepository> _logger;

    public AppointmentRepository(AppointmentDbContext context, ILogger<AppointmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Appointment Operations

    /// <summary>
    /// Get appointment by ID
    /// </summary>
    public async Task<AppointmentEntity?> GetAppointmentByIdAsync(Guid id)
    {
        try
        {
            return await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointment by ID: {AppointmentId}", id);
            throw new AppointmentException("Failed to get appointment by ID", innerException: ex);
        }
    }

    /// <summary>
    /// Create new appointment
    /// </summary>
    public async Task<AppointmentEntity> CreateAppointmentAsync(AppointmentEntity appointment)
    {
        try
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully created appointment: {AppointmentId}", appointment.Id);
            return appointment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment for patient: {PatientId}", appointment.PatientId);
            throw new AppointmentException("Failed to create appointment", innerException: ex);
        }
    }

    /// <summary>
    /// Get appointments with filtering, sorting and pagination
    /// </summary>
    public async Task<(List<AppointmentEntity> Appointments, int TotalCount)> GetAppointmentsAsync(AppointmentQueryRequest query, Role role)
    {
        try
        {
            var queryable = _context.Appointments.AsQueryable();

            // Apply role-based primary filters first
            switch (role)
            {
                case Role.PATIENT:
                    queryable = queryable.Where(a => a.PatientId == query.PatientId);
                    break;
                case Role.DOCTOR:
                    queryable = queryable.Where(a => a.DoctorId == query.DoctorId);
                    break;
                case Role.CLINIC:
                    queryable = queryable.Where(a => a.HospitalId == query.HospitalId);
                    break;
                default:
                    // Admin can see all appointments - no primary filter
                    break;
            }

            // Apply additional filters
            if (query.AppointmentType.HasValue)
                queryable = queryable.Where(a => a.AppointmentType == query.AppointmentType);

            if (query.Status.HasValue)
                queryable = queryable.Where(a => a.Status == query.Status);

            if (query.FromDate.HasValue)
                queryable = queryable.Where(a => a.AppointmentDate >= query.FromDate);

            if (query.ToDate.HasValue)
                queryable = queryable.Where(a => a.AppointmentDate <= query.ToDate);

            if (!string.IsNullOrWhiteSpace(query.SearchTerm))
            {
                var searchTerm = query.SearchTerm.ToLower();
                queryable = queryable.Where(a =>
                    a.Reason != null && a.Reason.ToLower().Contains(searchTerm) ||
                    a.Result != null && a.Result.ToLower().Contains(searchTerm));
            }

            var totalCount = await queryable.CountAsync();

            // Apply sorting
            queryable = query.SortBy?.ToLower() switch
            {
                "appointmentdate" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.AppointmentDate)
                    : queryable.OrderBy(a => a.AppointmentDate),
                "status" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.Status)
                    : queryable.OrderBy(a => a.Status),
                "appointmenttype" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.AppointmentType)
                    : queryable.OrderBy(a => a.AppointmentType),
                "updatedat" => query.SortDescending
                    ? queryable.OrderByDescending(a => a.UpdatedAt)
                    : queryable.OrderBy(a => a.UpdatedAt),
                _ => query.SortDescending
                    ? queryable.OrderByDescending(a => a.CreatedAt)
                    : queryable.OrderBy(a => a.CreatedAt)
            };

            // Apply pagination
            var appointments = await queryable
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} appointments out of {TotalCount}", appointments.Count, totalCount);
            return (appointments, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointments with query");
            throw new AppointmentException("Failed to get appointments", innerException: ex);
        }
    }

    /// <summary>
    /// Check if patient has conflicting appointment
    /// </summary>
    public async Task<bool> HasConflictingAppointmentAsync(Guid patientId, DateTime appointmentDate,
        Guid appointmentTimeId, Guid? excludeAppointmentId = null)
    {
        try
        {
            var query = _context.Appointments
                .Where(a => a.PatientId == patientId &&
                           a.AppointmentDate.Date == appointmentDate.Date &&
                           a.AppointmentTimeId == appointmentTimeId &&
                           a.Status != AppointmentStatus.CANCELLED);

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.Id != excludeAppointmentId);

            var hasConflict = await query.AnyAsync();

            if (hasConflict)
            {
                _logger.LogWarning("Conflicting appointment found for patient {PatientId} on {Date} at time {TimeId}",
                    patientId, appointmentDate.Date, appointmentTimeId);
            }

            return hasConflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for conflicting appointment: PatientId={PatientId}, Date={Date}, TimeId={TimeId}",
                patientId, appointmentDate.Date, appointmentTimeId);
            throw new AppointmentException("Failed to check for conflicting appointment", innerException: ex);
        }
    }

    /// <summary>
    /// Check if doctor is available
    /// </summary>
    public async Task<bool> IsDoctorAvailableAsync(Guid doctorId, DateTime appointmentDate,
        Guid appointmentTimeId, Guid? excludeAppointmentId = null)
    {
        try
        {
            var query = _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate.Date == appointmentDate.Date &&
                           a.AppointmentTimeId == appointmentTimeId &&
                           a.Status != AppointmentStatus.CANCELLED);

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.Id != excludeAppointmentId);

            var isAvailable = !await query.AnyAsync();

            if (!isAvailable)
            {
                _logger.LogWarning("Doctor {DoctorId} is not available on {Date} at time {TimeId}",
                    doctorId, appointmentDate.Date, appointmentTimeId);
            }

            return isAvailable;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking doctor availability: DoctorId={DoctorId}, Date={Date}, TimeId={TimeId}",
                doctorId, appointmentDate.Date, appointmentTimeId);
            throw new AppointmentException("Failed to check doctor availability", innerException: ex);
        }
    }

    #endregion

    #region Status Operations

    /// <summary>
    /// Update appointment status
    /// </summary>
    public async Task<bool> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus status, string? result = null)
    {
        try
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null)
            {
                _logger.LogWarning("Appointment not found for status update: {AppointmentId}", appointmentId);
                return false;
            }

            appointment.Status = status;
            if (!string.IsNullOrWhiteSpace(result))
                appointment.Result = result;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated appointment {AppointmentId} status to {Status}", appointmentId, status);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment status: {AppointmentId} to {Status}", appointmentId, status);
            throw new AppointmentException("Failed to update appointment status", innerException: ex);
        }
    }

    #endregion
}