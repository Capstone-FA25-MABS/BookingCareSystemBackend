using BookingCare.Services.Schedule.Data;
using BookingCare.Services.Schedule.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Schedule.Repositories;

/// <summary>
/// Repository implementation for schedule operations
/// </summary>
public class ScheduleRepository : IScheduleRepository
{
    private readonly ScheduleDbContext _context;

    public ScheduleRepository(ScheduleDbContext context)
    {
        _context = context;
    }

    #region AppointmentTime operations

    public async Task<AppointmentTimeEntity?> GetAppointmentTimeByIdAsync(long id)
    {
        return await _context.AppointmentTimes.FindAsync(id);
    }

    public async Task<IEnumerable<AppointmentTimeEntity>> GetAllAppointmentTimesAsync()
    {
        return await _context.AppointmentTimes
            .OrderBy(x => x.StartTime)
            .ToListAsync();
    }

    public async Task<AppointmentTimeEntity> CreateAppointmentTimeAsync(AppointmentTimeEntity appointmentTime)
    {
        _context.AppointmentTimes.Add(appointmentTime);
        await _context.SaveChangesAsync();
        return appointmentTime;
    }

    public async Task<AppointmentTimeEntity> UpdateAppointmentTimeAsync(AppointmentTimeEntity appointmentTime)
    {
        _context.AppointmentTimes.Update(appointmentTime);
        await _context.SaveChangesAsync();
        return appointmentTime;
    }

    public async Task DeleteAppointmentTimeAsync(long id)
    {
        var appointmentTime = await _context.AppointmentTimes.FindAsync(id);
        if (appointmentTime != null)
        {
            _context.AppointmentTimes.Remove(appointmentTime);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region SchedulePattern operations

    public async Task<SchedulePatternEntity?> GetSchedulePatternByIdAsync(long id)
    {
        return await _context.SchedulePatterns
            .Include(x => x.SchedulePatternSlots)
            .ThenInclude(x => x.AppointmentTime)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<IEnumerable<SchedulePatternEntity>> GetAllSchedulePatternsAsync()
    {
        return await _context.SchedulePatterns
            .Include(x => x.SchedulePatternSlots)
            .ThenInclude(x => x.AppointmentTime)
            .ToListAsync();
    }

    public async Task<SchedulePatternEntity> CreateSchedulePatternAsync(SchedulePatternEntity pattern, List<long> appointmentTimeIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            _context.SchedulePatterns.Add(pattern);
            await _context.SaveChangesAsync();

            foreach (var appointmentTimeId in appointmentTimeIds)
            {
                var slot = new SchedulePatternSlotEntity
                {
                    PatternId = pattern.Id,
                    AppointmentTimeId = appointmentTimeId
                };
                _context.SchedulePatternSlots.Add(slot);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetSchedulePatternByIdAsync(pattern.Id) ?? pattern;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SchedulePatternEntity> UpdateSchedulePatternAsync(SchedulePatternEntity pattern, List<long> appointmentTimeIds)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            _context.SchedulePatterns.Update(pattern);

            // Remove existing slots
            var existingSlots = await _context.SchedulePatternSlots
                .Where(x => x.PatternId == pattern.Id)
                .ToListAsync();
            _context.SchedulePatternSlots.RemoveRange(existingSlots);

            // Add new slots
            foreach (var appointmentTimeId in appointmentTimeIds)
            {
                var slot = new SchedulePatternSlotEntity
                {
                    PatternId = pattern.Id,
                    AppointmentTimeId = appointmentTimeId
                };
                _context.SchedulePatternSlots.Add(slot);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return await GetSchedulePatternByIdAsync(pattern.Id) ?? pattern;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteSchedulePatternAsync(long id)
    {
        var pattern = await _context.SchedulePatterns.FindAsync(id);
        if (pattern != null)
        {
            _context.SchedulePatterns.Remove(pattern);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region DoctorDailySchedule operations

    public async Task<DoctorDailyScheduleEntity?> GetDoctorDailyScheduleAsync(long doctorId, DateOnly date)
    {
        return await _context.DoctorDailySchedules
            .Include(x => x.Pattern)
            .ThenInclude(x => x.SchedulePatternSlots)
            .ThenInclude(x => x.AppointmentTime)
            .FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.ScheduleDate == date);
    }

    public async Task<IEnumerable<DoctorDailyScheduleEntity>> GetDoctorScheduleRangeAsync(long doctorId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.DoctorDailySchedules
            .Include(x => x.Pattern)
            .ThenInclude(x => x.SchedulePatternSlots)
            .ThenInclude(x => x.AppointmentTime)
            .Where(x => x.DoctorId == doctorId && x.ScheduleDate >= startDate && x.ScheduleDate <= endDate)
            .OrderBy(x => x.ScheduleDate)
            .ToListAsync();
    }

    public async Task<DoctorDailyScheduleEntity> CreateOrUpdateDoctorDailyScheduleAsync(DoctorDailyScheduleEntity schedule)
    {
        var existing = await _context.DoctorDailySchedules
            .FirstOrDefaultAsync(x => x.DoctorId == schedule.DoctorId && x.ScheduleDate == schedule.ScheduleDate);

        if (existing != null)
        {
            existing.PatternId = schedule.PatternId;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.DoctorDailySchedules.Update(existing);
        }
        else
        {
            _context.DoctorDailySchedules.Add(schedule);
        }

        await _context.SaveChangesAsync();
        return existing ?? schedule;
    }

    public async Task DeleteDoctorDailyScheduleAsync(long doctorId, DateOnly date)
    {
        var schedule = await _context.DoctorDailySchedules
            .FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.ScheduleDate == date);
        
        if (schedule != null)
        {
            _context.DoctorDailySchedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region DoctorScheduleException operations

    public async Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsAsync(long doctorId, DateOnly date)
    {
        return await _context.DoctorScheduleExceptions
            .Include(x => x.AppointmentTime)
            .Where(x => x.DoctorId == doctorId && x.ExceptionDate == date)
            .ToListAsync();
    }

    public async Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsRangeAsync(long doctorId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.DoctorScheduleExceptions
            .Include(x => x.AppointmentTime)
            .Where(x => x.DoctorId == doctorId && x.ExceptionDate >= startDate && x.ExceptionDate <= endDate)
            .OrderBy(x => x.ExceptionDate)
            .ToListAsync();
    }

    public async Task<DoctorScheduleExceptionEntity> CreateDoctorScheduleExceptionAsync(DoctorScheduleExceptionEntity exception)
    {
        _context.DoctorScheduleExceptions.Add(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task DeleteDoctorScheduleExceptionAsync(long id)
    {
        var exception = await _context.DoctorScheduleExceptions.FindAsync(id);
        if (exception != null)
        {
            _context.DoctorScheduleExceptions.Remove(exception);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region ClinicException operations

    public async Task<IEnumerable<ClinicExceptionEntity>> GetClinicExceptionsAsync(long clinicId, DateOnly date)
    {
        return await _context.ClinicExceptions
            .Where(x => x.ClinicId == clinicId && x.ExceptionDate == date)
            .ToListAsync();
    }

    public async Task<ClinicExceptionEntity> CreateClinicExceptionAsync(ClinicExceptionEntity exception)
    {
        _context.ClinicExceptions.Add(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task DeleteClinicExceptionAsync(long id)
    {
        var exception = await _context.ClinicExceptions.FindAsync(id);
        if (exception != null)
        {
            _context.ClinicExceptions.Remove(exception);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region ServiceSchedule operations

    public async Task<IEnumerable<ServiceScheduleEntity>> GetServiceSchedulesAsync(long serviceId)
    {
        return await _context.ServiceSchedules
            .Include(x => x.Pattern)
            .ThenInclude(x => x.SchedulePatternSlots)
            .ThenInclude(x => x.AppointmentTime)
            .Where(x => x.ServiceId == serviceId)
            .ToListAsync();
    }

    public async Task<ServiceScheduleEntity> CreateServiceScheduleAsync(ServiceScheduleEntity serviceSchedule)
    {
        _context.ServiceSchedules.Add(serviceSchedule);
        await _context.SaveChangesAsync();
        return serviceSchedule;
    }

    public async Task DeleteServiceScheduleAsync(long id)
    {
        var serviceSchedule = await _context.ServiceSchedules.FindAsync(id);
        if (serviceSchedule != null)
        {
            _context.ServiceSchedules.Remove(serviceSchedule);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Available slots operations

    public async Task<IEnumerable<AppointmentTimeEntity>> GetAvailableSlotsAsync(long doctorId, DateOnly date, long? serviceId = null)
    {
        // Get doctor's schedule for the day
        var doctorSchedule = await GetDoctorDailyScheduleAsync(doctorId, date);
        if (doctorSchedule?.Pattern == null)
        {
            return new List<AppointmentTimeEntity>();
        }

        // Get all slots from the pattern
        var availableSlots = doctorSchedule.Pattern.SchedulePatternSlots
            .Select(x => x.AppointmentTime)
            .ToList();

        // Get doctor's exceptions for the day
        var exceptions = await GetDoctorExceptionsAsync(doctorId, date);

        // Apply exceptions
        foreach (var exception in exceptions)
        {
            if (exception.ExceptionType == ExceptionType.DAY_OFF)
            {
                // Doctor is off for the entire day
                return new List<AppointmentTimeEntity>();
            }
            else if (exception.AppointmentTimeId.HasValue)
            {
                if (exception.ExceptionType == ExceptionType.BLOCK_SLOT && !exception.IsAvailable)
                {
                    // Remove blocked slot
                    availableSlots.RemoveAll(x => x.Id == exception.AppointmentTimeId.Value);
                }
                else if (exception.ExceptionType == ExceptionType.UNBLOCK_SLOT && exception.IsAvailable)
                {
                    // Add unblocked slot if not already present
                    var slotToAdd = await GetAppointmentTimeByIdAsync(exception.AppointmentTimeId.Value);
                    if (slotToAdd != null && !availableSlots.Any(x => x.Id == slotToAdd.Id))
                    {
                        availableSlots.Add(slotToAdd);
                    }
                }
            }
        }

        // If service is specified, filter by service schedule
        if (serviceId.HasValue)
        {
            var serviceSchedules = await GetServiceSchedulesAsync(serviceId.Value);
            var serviceSlots = serviceSchedules
                .SelectMany(x => x.Pattern.SchedulePatternSlots)
                .Select(x => x.AppointmentTime)
                .ToList();

            // Only return slots that are available for both doctor and service
            availableSlots = availableSlots
                .Where(slot => serviceSlots.Any(serviceSlot => serviceSlot.Id == slot.Id))
                .ToList();
        }

        return availableSlots.OrderBy(x => x.StartTime);
    }

    #endregion
}