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

        // Statuses takes precedence over Status for filtering multiple statuses
        // Used for scenarios like calendar view showing only CONFIRMED and COMPLETED
        if (query.Statuses != null && query.Statuses.Any())
        {
            queryable = queryable.Where(a => query.Statuses.Contains(a.Status));
        }
        else if (query.Status.HasValue)
        {
            queryable = queryable.Where(a => a.Status == query.Status);
        }

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

        // Filter by relative (for patient relatives feature)
        if (query.ForRelative.HasValue)
        {
            if (query.ForRelative.Value)
            {
                // Only appointments for relatives (RelativeId is not null)
                queryable = queryable.Where(a => a.RelativeId != null);
            }
            else
            {
                // Only appointments for self (RelativeId is null)
                queryable = queryable.Where(a => a.RelativeId == null);
            }
        }

        // Search by appointment ID, reason, or symptoms
        // Note: Doctor/Hospital/Service names are searched client-side after gRPC enrichment
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim().ToLower();
            queryable = queryable.Where(a =>
                a.Id.ToString().ToLower().Contains(searchTerm) ||
                (a.Reason != null && a.Reason.ToLower().Contains(searchTerm)) ||
                (a.Symptoms != null && a.Symptoms.ToLower().Contains(searchTerm)) ||
                (a.Result != null && a.Result.ToLower().Contains(searchTerm))
            );
        }

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
    /// Check if patient or relative has conflicting appointment
    /// </summary>
    /// <param name="patientId">Patient ID (user who is booking)</param>
    /// <param name="appointmentDate">Appointment date</param>
    /// <param name="appointmentTimeId">Appointment time slot</param>
    /// <param name="relativeId">Relative ID if booking for family member (null = booking for self)</param>
    /// <param name="excludeAppointmentId">Appointment ID to exclude (for reschedule)</param>
    public async Task<bool> HasConflictingAppointmentAsync(Guid patientId, DateTime appointmentDate,
        AppointmentTime appointmentTimeId, Guid? relativeId = null, Guid? excludeAppointmentId = null)
    {
        try
        {
            IQueryable<AppointmentEntity> query;

            if (relativeId.HasValue)
            {
                // Booking for relative - check if this relative already has appointment at this time
                query = _context.Appointments
                    .Where(a => a.RelativeId == relativeId.Value &&
                               a.AppointmentDate.Date == appointmentDate.Date &&
                               a.AppointmentTimeId == appointmentTimeId &&
                               a.Status != AppointmentStatus.CANCELLED);
            }
            else
            {
                // Booking for self - check if patient already has appointment at this time (without relative)
                query = _context.Appointments
                    .Where(a => a.PatientId == patientId &&
                               a.RelativeId == null &&
                               a.AppointmentDate.Date == appointmentDate.Date &&
                               a.AppointmentTimeId == appointmentTimeId &&
                               a.Status != AppointmentStatus.CANCELLED);
            }

            if (excludeAppointmentId.HasValue)
                query = query.Where(a => a.Id != excludeAppointmentId);

            var hasConflict = await query.AnyAsync();

            if (hasConflict)
            {
                if (relativeId.HasValue)
                {
                    _logger.LogWarning("Conflicting appointment found for relative {RelativeId} on {Date} at time {TimeId}",
                        relativeId, appointmentDate.Date, appointmentTimeId);
                }
                else
                {
                    _logger.LogWarning("Conflicting appointment found for patient {PatientId} on {Date} at time {TimeId}",
                        patientId, appointmentDate.Date, appointmentTimeId);
                }
            }

            return hasConflict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for conflicting appointment: PatientId={PatientId}, RelativeId={RelativeId}, Date={Date}, TimeId={TimeId}",
                patientId, relativeId, appointmentDate.Date, appointmentTimeId);
            throw new AppointmentException("Failed to check for conflicting appointment", innerException: ex);
        }
    }

    /// <summary>
    /// Check if doctor is available (includes soft reservation check)
    /// Doctor is NOT available if:
    /// 1. Has active appointment (PENDING/CONFIRMED)
    /// 2. OR has soft reservation (AssignedDoctorId with valid SoftReservedUntil)
    /// </summary>
    public async Task<bool> IsDoctorAvailableAsync(Guid doctorId, DateTime appointmentDate,
        AppointmentTime appointmentTimeId, Guid? excludeAppointmentId = null)
    {
        try
        {
            var now = DateTime.UtcNow;

            // Check 1: Active appointments with this doctor
            var hasActiveAppointment = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate.Date == appointmentDate.Date &&
                           a.AppointmentTimeId == appointmentTimeId &&
                           a.Status != AppointmentStatus.CANCELLED &&
                           (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value))
                .AnyAsync();

            if (hasActiveAppointment)
            {
                _logger.LogWarning("Doctor {DoctorId} has active appointment on {Date} at {TimeId}",
                    doctorId, appointmentDate.Date, appointmentTimeId);
                return false;
            }

            // Check 2: Soft reservations (pending doctor assignment)
            var hasSoftReservation = await _context.Appointments
                .Where(a => a.AssignedDoctorId == doctorId &&
                           a.AppointmentDate.Date == appointmentDate.Date &&
                           a.AppointmentTimeId == appointmentTimeId &&
                           a.Status == AppointmentStatus.CANCELLED &&
                           a.SoftReservedUntil.HasValue &&
                           a.SoftReservedUntil.Value > now &&
                           (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value))
                .AnyAsync();

            if (hasSoftReservation)
            {
                _logger.LogWarning("Doctor {DoctorId} has soft reservation on {Date} at {TimeId}",
                    doctorId, appointmentDate.Date, appointmentTimeId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking doctor availability: DoctorId={DoctorId}, Date={Date}, TimeId={TimeId}",
                doctorId, appointmentDate.Date, appointmentTimeId);
            throw new AppointmentException("Failed to check doctor availability", innerException: ex);
        }
    }

    /// <summary>
    /// Check if service medical slot is available
    /// Service is NOT available if there's already an active appointment (PENDING/CONFIRMED) for that slot
    /// </summary>
    public async Task<bool> IsServiceMedicalAvailableAsync(Guid serviceId, DateTime appointmentDate,
        AppointmentTime appointmentTimeId, Guid? excludeAppointmentId = null)
    {
        try
        {
            // Check for active appointments with this service at the same time slot
            var hasActiveAppointment = await _context.Appointments
                .Where(a => a.ServiceId == serviceId &&
                           a.AppointmentDate.Date == appointmentDate.Date &&
                           a.AppointmentTimeId == appointmentTimeId &&
                           a.Status != AppointmentStatus.CANCELLED &&
                           (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value))
                .AnyAsync();

            if (hasActiveAppointment)
            {
                _logger.LogWarning("Service {ServiceId} has active appointment on {Date} at {TimeId}",
                    serviceId, appointmentDate.Date, appointmentTimeId);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service medical availability: ServiceId={ServiceId}, Date={Date}, TimeId={TimeId}",
                serviceId, appointmentDate.Date, appointmentTimeId);
            throw new AppointmentException("Failed to check service medical availability", innerException: ex);
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
    /// Update an existing appointment (for reschedule operations)
    /// </summary>
    public async Task<bool> UpdateAppointmentAsync(AppointmentEntity appointment)
    {
        try
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated appointment: {AppointmentId}", appointment.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment: {AppointmentId}", appointment.Id);
            throw new AppointmentException("Failed to update appointment", innerException: ex);
        }
    }

    public async Task<List<AppointmentEntity>> GetAppointmentsWithExpiredTokensAsync(DateTime now)
    {
        try
        {
            var appointments = await _context.Appointments
                .Where(a => a.RescheduleToken != null &&
                           a.RescheduleTokenExpiry != null &&
                           a.RescheduleTokenExpiry < now)
                .ToListAsync();

            _logger.LogInformation("Found {Count} appointments with expired tokens", appointments.Count);
            return appointments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching appointments with expired tokens");
            throw new AppointmentException("Failed to fetch appointments with expired tokens", innerException: ex);
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
    /// Get counts for all appointment statuses for a specific user or organization using a single optimized query.
    /// All filters are encapsulated in <see cref="AppointmentStatusFilter"/> for better readability.
    /// </summary>
    public async Task<Dictionary<AppointmentStatus, int>> GetStatusCountsByUserAsync(
        AppointmentStatusFilter filter)
    {
        try
        {
            var query = _context.Appointments.AsQueryable();

            // Apply appropriate filter based on role
            if (filter.CountAll)
            {
                // ADMIN role: Count all appointments (no filter)
                _logger.LogInformation("Counting all appointments for ADMIN role");
            }
            else if (filter.PatientId.HasValue)
            {
                // PATIENT role: Filter by patient
                query = query.Where(a => a.PatientId == filter.PatientId.Value);
                _logger.LogInformation("Counting appointments for PatientId: {PatientId}", filter.PatientId.Value);
            }
            else if (filter.DoctorId.HasValue)
            {
                // DOCTOR role: Filter by doctor
                query = query.Where(a => a.DoctorId == filter.DoctorId.Value);
                _logger.LogInformation("Counting appointments for DoctorId: {DoctorId}", filter.DoctorId.Value);
            }
            else if (filter.HospitalId.HasValue)
            {
                // STAFF role: Filter by hospital
                query = query.Where(a => a.HospitalId == filter.HospitalId.Value);
                _logger.LogInformation("Counting appointments for HospitalId: {HospitalId}", filter.HospitalId.Value);
            }
            else
            {
                // No valid filter provided
                _logger.LogWarning("No valid filter provided for status counts query");
                return new Dictionary<AppointmentStatus, int>();
            }

            query = ApplyStatusFilter(query, filter);

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
                filter.PatientId, filter.DoctorId, filter.HospitalId, filter.CountAll, result[AppointmentStatus.PENDING], result[AppointmentStatus.CONFIRMED],
                result[AppointmentStatus.CANCELLED], result[AppointmentStatus.COMPLETED]);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status counts for user (PatientId: {PatientId}, DoctorId: {DoctorId})", patientId, doctorId);
            throw new AppointmentException("Failed to get status counts", innerException: ex);
        }
    }

    private static IQueryable<AppointmentEntity> ApplyStatusFilter(
        IQueryable<AppointmentEntity> query,
        AppointmentStatusFilter filter)
    {
        if (filter.AppointmentType.HasValue)
        {
            query = query.Where(a => a.AppointmentType == filter.AppointmentType.Value);
        }

        if (filter.FromDate.HasValue)
        {
            var fromDateValue = filter.FromDate.Value.Date;
            query = query.Where(a => a.AppointmentDate >= fromDateValue);
        }

        if (filter.ToDate.HasValue)
        {
            var toDateValue = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.AppointmentDate <= toDateValue);
        }

        if (filter.ForRelative.HasValue)
        {
            query = filter.ForRelative.Value
                ? query.Where(a => a.RelativeId != null)
                : query.Where(a => a.RelativeId == null);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(a =>
                a.Id.ToString().ToLower().Contains(term) ||
                (a.Reason != null && a.Reason.ToLower().Contains(term)) ||
                (a.Symptoms != null && a.Symptoms.ToLower().Contains(term)) ||
                (a.Result != null && a.Result.ToLower().Contains(term)));
        }

        return query;
    }

    public async Task<List<AppointmentEntity>> GetAppointmentsForHospitalAsync(Guid hospitalId, DateTime fromDate, DateTime toDate)
    {
        try
        {
            return await _context.Appointments
                .AsNoTracking()
                .Where(a =>
                    a.HospitalId.HasValue &&
                    a.HospitalId == hospitalId &&
                    a.AppointmentDate >= fromDate &&
                    a.AppointmentDate <= toDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointments for hospital {HospitalId}", hospitalId);
            throw new AppointmentException("Failed to get hospital appointments", innerException: ex);
        }
    }

    public async Task<Dictionary<Guid, DateTime>> GetPatientFirstAppointmentsAsync(Guid hospitalId)
    {
        try
        {
            var query = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.HospitalId.HasValue && a.HospitalId == hospitalId)
                .GroupBy(a => a.PatientId)
                .Select(g => new
                {
                    PatientId = g.Key,
                    FirstAppointmentAt = g.Min(a => a.CreatedAt)
                })
                .ToListAsync();

            return query.ToDictionary(x => x.PatientId, x => x.FirstAppointmentAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting patient first appointments for hospital {HospitalId}", hospitalId);
            throw new AppointmentException("Failed to get patient first appointments", innerException: ex);
        }
    }

    /// <summary>
    /// Get all booked appointment time IDs for a doctor on a specific date
    /// Returns appointments with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    public async Task<List<AppointmentTime>> GetBookedAppointmentTimesAsync(Guid doctorId, DateOnly appointmentDate)
    {
        try
        {
            var startOfDay = appointmentDate.ToDateTime(TimeOnly.MinValue);
            var endOfDay = appointmentDate.ToDateTime(TimeOnly.MaxValue);

            var bookedTimeIds = await _context.Appointments
                .Where(a => a.DoctorId == doctorId &&
                           a.AppointmentDate >= startOfDay &&
                           a.AppointmentDate <= endOfDay &&
                           (a.Status == AppointmentStatus.PENDING ||
                            a.Status == AppointmentStatus.CONFIRMED ||
                            a.Status == AppointmentStatus.COMPLETED))
                .Select(a => a.AppointmentTimeId)
                .Distinct()
                .ToListAsync();

            _logger.LogDebug("Found {Count} booked time slots for doctor {DoctorId} on {Date}",
                bookedTimeIds.Count, doctorId, appointmentDate);

            return bookedTimeIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booked appointment times for doctor {DoctorId} on {Date}",
                doctorId, appointmentDate);
            throw new AppointmentException("Failed to get booked appointment times", innerException: ex);
        }
    }

    /// <summary>
    /// Get all booked appointment time IDs for a service medical on a specific date
    /// Returns appointments with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    public async Task<List<AppointmentTime>> GetBookedAppointmentTimesByServiceAsync(Guid serviceId, DateOnly appointmentDate)
    {
        try
        {
            var startOfDay = appointmentDate.ToDateTime(TimeOnly.MinValue);
            var endOfDay = appointmentDate.ToDateTime(TimeOnly.MaxValue);

            var bookedTimeIds = await _context.Appointments
                .Where(a => a.ServiceId == serviceId &&
                           a.AppointmentDate >= startOfDay &&
                           a.AppointmentDate <= endOfDay &&
                           (a.Status == AppointmentStatus.PENDING ||
                            a.Status == AppointmentStatus.CONFIRMED ||
                            a.Status == AppointmentStatus.COMPLETED))
                .Select(a => a.AppointmentTimeId)
                .Distinct()
                .ToListAsync();

            _logger.LogDebug("Found {Count} booked time slots for service {ServiceId} on {Date}",
                bookedTimeIds.Count, serviceId, appointmentDate);

            return bookedTimeIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booked appointment times for service {ServiceId} on {Date}",
                serviceId, appointmentDate);
            throw new AppointmentException("Failed to get booked appointment times for service", innerException: ex);
        }
    }

    /// <summary>
    /// Get booked slot counts for a specialty (hospital assigns doctor mode)
    /// Returns count of PENDING/CONFIRMED appointments per time slot
    /// This is used to calculate remaining capacity for specialty booking
    /// </summary>
    public async Task<Dictionary<AppointmentTime, int>> GetSpecialtyBookedSlotCountsAsync(
        Guid hospitalId,
        Guid specialtyId,
        DateOnly appointmentDate,
        AppointmentType appointmentType)
    {
        try
        {
            var startOfDay = appointmentDate.ToDateTime(TimeOnly.MinValue);
            var endOfDay = appointmentDate.ToDateTime(TimeOnly.MaxValue);

            // Query appointments that match:
            // - Same hospital, specialty, date, and appointment type
            // - Status is PENDING or CONFIRMED (not CANCELLED, COMPLETED, etc.)
            // - These are specialty bookings where hospital assigns doctor
            var bookedCounts = await _context.Appointments
                .Where(a => a.HospitalId == hospitalId &&
                           a.SpecialtyId == specialtyId &&
                           a.AppointmentDate >= startOfDay &&
                           a.AppointmentDate <= endOfDay &&
                           a.AppointmentType == appointmentType &&
                           a.Status == AppointmentStatus.PENDING)
                .GroupBy(a => a.AppointmentTimeId)
                .Select(g => new { AppointmentTimeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.AppointmentTimeId, x => x.Count);

            _logger.LogDebug(
                "Found {SlotCount} time slots with {TotalCount} total bookings for specialty {SpecialtyId} at hospital {HospitalId} on {Date}",
                bookedCounts.Count,
                bookedCounts.Values.Sum(),
                specialtyId,
                hospitalId,
                appointmentDate);

            return bookedCounts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting specialty booked slot counts for hospital {HospitalId}, specialty {SpecialtyId} on {Date}",
                hospitalId, specialtyId, appointmentDate);
            throw new AppointmentException("Failed to get specialty booked slot counts", innerException: ex);
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

    /// <summary>
    /// NEW: Get completed appointments by patient with optional doctor or service filter (for Review service validation)
    /// Returns appointments with COMPLETED status for appointment history validation
    /// </summary>
    public async Task<List<AppointmentEntity>> GetCompletedAppointmentsByPatientAsync(
        Guid patientId,
        Guid? doctorId = null,
        Guid? serviceId = null)
    {
        try
        {
            var query = _context.Appointments
                .Where(a => a.PatientId == patientId && a.Status == AppointmentStatus.COMPLETED);

            // Filter by doctor if specified
            if (doctorId.HasValue)
            {
                query = query.Where(a => a.DoctorId == doctorId.Value);
            }

            // Filter by service if specified
            if (serviceId.HasValue)
            {
                query = query.Where(a => a.ServiceId == serviceId.Value);
            }

            var completedAppointments = await query.ToListAsync();

            // Extract nested ternary operation into independent statement
            var targetInfo = GetTargetInfoForLogging(doctorId, serviceId);

            _logger.LogInformation(
                "Found {Count} completed appointments for patient {PatientId} with {Target}",
                completedAppointments.Count, patientId, targetInfo);

            return completedAppointments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting completed appointments for patient {PatientId}, doctor {DoctorId}, service {ServiceId}",
                patientId, doctorId ?? Guid.Empty, serviceId ?? Guid.Empty);
            throw new AppointmentException("Failed to get completed appointments by patient", innerException: ex);
        }
    }

    /// <summary>
    /// Helper method to generate target info string for logging purposes
    /// </summary>
    private static string GetTargetInfoForLogging(Guid? doctorId, Guid? serviceId)
    {
        if (doctorId.HasValue)
        {
            return $"doctor {doctorId}";
        }

        if (serviceId.HasValue)
        {
            return $"service {serviceId}";
        }

        return "any target";
    }

    #endregion

    #region Assign Doctor To Appointment (NEW flow)

    /// <summary>
    /// Get completed appointments for a patient at a specific hospital and specialty
    /// Used to find previous doctors who treated this patient
    /// Note: patientId can be either PatientId (booking for self) or PatientRelativeId (booking for family member)
    /// We search both fields to find all appointments for this actual patient
    /// </summary>
    public async Task<List<AppointmentEntity>> GetCompletedAppointmentsForPatientAsync(
        Guid patientId,
        Guid hospitalId,
        Guid specialtyId)
    {
        try
        {
            // Search for appointments where:
            // 1. PatientId matches (patient booked for themselves)
            // 2. OR PatientRelativeId matches (someone booked for this patient as a relative)
            return await _context.Appointments
                .Where(a => (a.PatientId == patientId || a.RelativeId == patientId)
                    && a.HospitalId == hospitalId
                    && a.SpecialtyId == specialtyId
                    && a.Status == AppointmentStatus.COMPLETED
                    && a.DoctorId.HasValue)
                .OrderByDescending(a => a.AppointmentDate)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting completed appointments for patient {PatientId}, hospital {HospitalId}, specialty {SpecialtyId}",
                patientId, hospitalId, specialtyId);
            throw new AppointmentException("Failed to get completed appointments for patient", innerException: ex);
        }
    }

    /// <summary>
    /// Get booking counts (completed appointments) for multiple doctors
    public async Task<Dictionary<Guid, int>> GetDoctorBookingCountsAsync(List<Guid> doctorIds)
    {
        try
        {
            if (!doctorIds.Any())
            {
                return new Dictionary<Guid, int>();
            }

            var counts = await _context.Appointments
                .Where(a => a.DoctorId.HasValue
                    && doctorIds.Contains(a.DoctorId.Value)
                    && a.Status == AppointmentStatus.COMPLETED)
                .GroupBy(a => a.DoctorId!.Value)
                .Select(g => new { DoctorId = g.Key, Count = g.Count() })
                .ToListAsync();

            return counts.ToDictionary(x => x.DoctorId, x => x.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking counts for {Count} doctors", doctorIds.Count);
            throw new AppointmentException("Failed to get doctor booking counts", innerException: ex);
        }
    }

    /// <summary>
    /// Get doctor IDs that have booked slots at a specific date/time
    /// Used to check availability
    /// </summary>
    public async Task<List<Guid>> GetDoctorsWithBookedSlotAsync(
        List<Guid> doctorIds,
        DateOnly date,
        AppointmentTime appointmentTimeId)
    {
        try
        {
            if (!doctorIds.Any())
            {
                return new List<Guid>();
            }

            var dateTime = date.ToDateTime(TimeOnly.MinValue);

            return await _context.Appointments
                .Where(a => a.DoctorId.HasValue
                    && doctorIds.Contains(a.DoctorId.Value)
                    && a.AppointmentDate.Date == dateTime.Date
                    && a.AppointmentTimeId == appointmentTimeId
                    && (a.Status == AppointmentStatus.PENDING
                        || a.Status == AppointmentStatus.CONFIRMED
                        || a.Status == AppointmentStatus.COMPLETED))
                .Select(a => a.DoctorId!.Value)
                .Distinct()
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error getting doctors with booked slot on {Date} at {TimeId}",
                date, appointmentTimeId);
            throw new AppointmentException("Failed to get doctors with booked slot", innerException: ex);
        }
    }

    #endregion
}