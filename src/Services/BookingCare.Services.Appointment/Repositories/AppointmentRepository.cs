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

            queryable = ApplyRoleBasedFilter(queryable, query, role);
            queryable = ApplyAdditionalFilters(queryable, query);

            var totalCount = await queryable.CountAsync();

            queryable = ApplySorting(queryable, query);

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

    private static IQueryable<AppointmentEntity> ApplyRoleBasedFilter(IQueryable<AppointmentEntity> queryable, AppointmentQueryRequest query, Role role)
    {
        return role switch
        {
            Role.PATIENT => queryable.Where(a => a.PatientId == query.PatientId),
            Role.DOCTOR => queryable.Where(a => a.DoctorId == query.DoctorId),
            Role.STAFF => queryable.Where(a => a.HospitalId == query.HospitalId),
            _ => queryable // Admin can see all appointments
        };
    }

    private static IQueryable<AppointmentEntity> ApplyAdditionalFilters(IQueryable<AppointmentEntity> queryable, AppointmentQueryRequest query)
    {
        if (query.AppointmentType.HasValue)
            queryable = queryable.Where(a => a.AppointmentType == query.AppointmentType);

        if (query.Status.HasValue)
            queryable = queryable.Where(a => a.Status == query.Status);

        // Date range filtering
        if (query.FromDate.HasValue)
        {
            // Include appointments from the start of FromDate
            var fromDate = query.FromDate.Value.Date;
            queryable = queryable.Where(a => a.AppointmentDate >= fromDate);
        }

        if (query.ToDate.HasValue)
        {
            // Include appointments until the end of ToDate (23:59:59)
            var toDate = query.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            queryable = queryable.Where(a => a.AppointmentDate <= toDate);
        }

        // Note: SearchTerm is handled client-side in frontend for better UX
        // (allows searching doctor/hospital/service names from gRPC data)

        return queryable;
    }

    private static IQueryable<AppointmentEntity> ApplySorting(IQueryable<AppointmentEntity> queryable, AppointmentQueryRequest query)
    {
        return query.SortBy?.ToLower() switch
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
    }

    /// <summary>
    /// Check if patient has conflicting appointment
    /// </summary>
    public async Task<bool> HasConflictingAppointmentAsync(Guid patientId, DateTime appointmentDate,
        AppointmentTime appointmentTimeId, Guid? excludeAppointmentId = null)
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
        AppointmentTime appointmentTimeId, Guid? excludeAppointmentId = null)
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

    /// <summary>
    /// Cancel an appointment with cancellation reason
    /// Optimized method that takes the full entity to avoid additional DB query
    /// </summary>
    public async Task<bool> CancelAppointmentAsync(AppointmentEntity appointment, string cancellationReason, string cancelledBy)
    {
        try
        {
            appointment.Status = AppointmentStatus.CANCELLED;
            appointment.Reason = cancellationReason;
            appointment.CancelledBy = cancelledBy;
            appointment.CancelledAt = DateTime.UtcNow;

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully cancelled appointment {AppointmentId} with reason: {Reason}, CancelledBy: {CancelledBy}",
                appointment.Id, cancellationReason, cancelledBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling appointment: {AppointmentId}", appointment.Id);
            throw new AppointmentException("Failed to cancel appointment", innerException: ex);
        }
    }

    /// <summary>
    /// Delete an appointment completely from the database
    /// Used when payment fails to free up the time slot completely
    /// </summary>
    public async Task<bool> DeleteAppointmentAsync(AppointmentEntity appointment)
    {
        try
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully deleted appointment {AppointmentId} from database - PatientId: {PatientId}, DoctorId: {DoctorId}, Date: {Date}",
                appointment.Id, appointment.PatientId, appointment.DoctorId, appointment.AppointmentDate);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting appointment: {AppointmentId}", appointment.Id);
            throw new AppointmentException("Failed to delete appointment", innerException: ex);
        }
    }

    #endregion

    #region Statistics Operations

    /// <summary>
    /// Get counts for all appointment statuses for a specific user or organization using a single optimized query
    /// Supports filtering by PatientId, DoctorId, HospitalId, or all (for ADMIN)
    /// </summary>
    public async Task<Dictionary<AppointmentStatus, int>> GetStatusCountsByUserAsync(
        Guid? patientId = null,
        Guid? doctorId = null,
        Guid? hospitalId = null,
        bool countAll = false)
    {
        try
        {
            var query = _context.Appointments.AsQueryable();

            // Apply appropriate filter based on role
            if (countAll)
            {
                // ADMIN role: Count all appointments (no filter)
                _logger.LogInformation("Counting all appointments for ADMIN role");
            }
            else if (patientId.HasValue)
            {
                // PATIENT role: Filter by patient
                query = query.Where(a => a.PatientId == patientId.Value);
                _logger.LogInformation("Counting appointments for PatientId: {PatientId}", patientId.Value);
            }
            else if (doctorId.HasValue)
            {
                // DOCTOR role: Filter by doctor
                query = query.Where(a => a.DoctorId == doctorId.Value);
                _logger.LogInformation("Counting appointments for DoctorId: {DoctorId}", doctorId.Value);
            }
            else if (hospitalId.HasValue)
            {
                // STAFF role: Filter by hospital
                query = query.Where(a => a.HospitalId == hospitalId.Value);
                _logger.LogInformation("Counting appointments for HospitalId: {HospitalId}", hospitalId.Value);
            }
            else
            {
                // No valid filter provided
                _logger.LogWarning("No valid filter provided for status counts query");
                return new Dictionary<AppointmentStatus, int>();
            }

            // Group by status and count - single DB query
            var statusCounts = await query
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            // Convert to dictionary with all statuses (including 0 counts)
            var result = new Dictionary<AppointmentStatus, int>
            {
                { AppointmentStatus.PENDING, 0 },
                { AppointmentStatus.CONFIRMED, 0 },
                { AppointmentStatus.CANCELLED, 0 },
                { AppointmentStatus.COMPLETED, 0 }
            };

            foreach (var item in statusCounts)
            {
                result[item.Status] = item.Count;
            }

            _logger.LogInformation(
                "Retrieved status counts (PatientId: {PatientId}, DoctorId: {DoctorId}, HospitalId: {HospitalId}, CountAll: {CountAll}): Pending={Pending}, Confirmed={Confirmed}, Cancelled={Cancelled}, Completed={Completed}",
                patientId, doctorId, hospitalId, countAll, result[AppointmentStatus.PENDING], result[AppointmentStatus.CONFIRMED],
                result[AppointmentStatus.CANCELLED], result[AppointmentStatus.COMPLETED]);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status counts for user (PatientId: {PatientId}, DoctorId: {DoctorId})", patientId, doctorId);
            throw new AppointmentException("Failed to get status counts", innerException: ex);
        }
    }

    #endregion

    #region Background Service Operations

    /// <summary>
    /// Get overdue appointments by status
    /// Returns appointments where AppointmentDate is before the reference date
    /// </summary>
    public async Task<List<AppointmentEntity>> GetOverdueAppointmentsByStatusAsync(
        AppointmentStatus status,
        DateTime referenceDate)
    {
        try
        {
            var overdueAppointments = await _context.Appointments
                .Where(a => a.Status == status && a.AppointmentDate.Date < referenceDate.Date)
                .ToListAsync();

            _logger.LogInformation(
                "Found {Count} overdue appointments with status {Status} before {ReferenceDate}",
                overdueAppointments.Count, status, referenceDate.Date);

            return overdueAppointments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting overdue appointments by status: {Status}, ReferenceDate: {ReferenceDate}",
                status, referenceDate.Date);
            throw new AppointmentException("Failed to get overdue appointments", innerException: ex);
        }
    }

    #endregion
}