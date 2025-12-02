using BookingCare.Services.Schedule.Data;
using BookingCare.Services.Schedule.Enums;
using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Shared.Common.Enums;
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



    #region DoctorDailySchedule operations

    public async Task<DoctorDailyScheduleEntity?> GetDoctorDailyScheduleAsync(Guid doctorId, DateOnly date)
    {
        return await _context.DoctorDailySchedules
            .FirstOrDefaultAsync(x => x.DoctorId == doctorId && x.ScheduleDate == date);
    }

    public async Task<IEnumerable<DoctorDailyScheduleEntity>> GetDoctorScheduleRangeAsync(Guid doctorId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.DoctorDailySchedules
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
            existing.SchedulePatterns = schedule.SchedulePatterns;
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

    public async Task DeleteDoctorDailyScheduleAsync(Guid doctorId, DateOnly date)
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

    public async Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsAsync(Guid doctorId, DateOnly date)
    {
        return await _context.DoctorScheduleExceptions
            .Where(x => x.DoctorId == doctorId && x.ExceptionDate == date)
            .ToListAsync();
    }

    public async Task<IEnumerable<DoctorScheduleExceptionEntity>> GetDoctorExceptionsRangeAsync(Guid doctorId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.DoctorScheduleExceptions
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

    public async Task DeleteDoctorScheduleExceptionAsync(Guid id)
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

    public async Task<IEnumerable<ClinicExceptionEntity>> GetClinicExceptionsAsync(Guid clinicId, DateOnly date)
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

    public async Task DeleteClinicExceptionAsync(Guid id)
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

    public async Task<IEnumerable<ServiceScheduleEntity>> GetServiceSchedulesAsync(Guid serviceId)
    {
        return await _context.ServiceSchedules
            .Where(x => x.ServiceId == serviceId)
            .ToListAsync();
    }

    public async Task<ServiceScheduleEntity> CreateServiceScheduleAsync(ServiceScheduleEntity serviceSchedule)
    {
        _context.ServiceSchedules.Add(serviceSchedule);
        await _context.SaveChangesAsync();
        return serviceSchedule;
    }

    public async Task DeleteServiceScheduleAsync(Guid id)
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

    public async Task<IEnumerable<AppointmentTime>> GetAvailableSlotsAsync(Guid doctorId, DateOnly date, Guid? serviceId = null)
    {
        // Get doctor's schedule for the day
        var doctorSchedule = await GetDoctorDailyScheduleAsync(doctorId, date);
        if (doctorSchedule == null)
        {
            return new List<AppointmentTime>();
        }

        // Get all slots from the patterns based on enum collection
        var availableSlots = GetAppointmentTimesForPatterns(doctorSchedule.SchedulePatterns).ToList();

        // Get doctor's exceptions for the day
        var exceptions = await GetDoctorExceptionsAsync(doctorId, date);

        // Apply exceptions to filter available slots
        availableSlots = ApplyExceptionsToSlots(availableSlots, exceptions);

        // If service is specified, filter by service schedule
        if (serviceId.HasValue)
        {
            availableSlots = await FilterSlotsByService(availableSlots, serviceId.Value);
        }

        return availableSlots.OrderBy(x => (int)x);
    }

    public async Task<Dictionary<Guid, List<AppointmentTime>>> GetAvailableSlotsForDoctorsAsync(List<Guid> doctorIds, DateOnly date)
    {
        var result = new Dictionary<Guid, List<AppointmentTime>>();

        if (!doctorIds.Any())
        {
            return result;
        }

        // Step 1: Batch query all doctor schedules for the date
        var doctorSchedules = await _context.DoctorDailySchedules
            .Where(s => doctorIds.Contains(s.DoctorId) && s.ScheduleDate == date)
            .ToListAsync();

        // Step 2: Batch query all doctor exceptions for the date
        var doctorExceptions = await _context.DoctorScheduleExceptions
            .Where(e => doctorIds.Contains(e.DoctorId) && e.ExceptionDate == date)
            .ToListAsync();

        // Group by doctor for efficient lookup
        var schedulesByDoctor = doctorSchedules.ToDictionary(s => s.DoctorId);
        var exceptionsByDoctor = doctorExceptions.GroupBy(e => e.DoctorId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Step 3: Process each doctor's available slots
        foreach (var doctorId in doctorIds)
        {
            if (!schedulesByDoctor.TryGetValue(doctorId, out var schedule))
            {
                result[doctorId] = new List<AppointmentTime>();
                continue;
            }

            // Get all slots from the patterns
            var availableSlots = GetAppointmentTimesForPatterns(schedule.SchedulePatterns).ToList();

            // Apply exceptions if any
            if (exceptionsByDoctor.TryGetValue(doctorId, out var exceptions))
            {
                availableSlots = ApplyExceptionsToSlots(availableSlots, exceptions);
            }

            result[doctorId] = availableSlots.OrderBy(x => (int)x).ToList();
        }

        return result;
    }

    private static List<AppointmentTime> ApplyExceptionsToSlots(List<AppointmentTime> availableSlots, IEnumerable<DoctorScheduleExceptionEntity> exceptions)
    {
        foreach (var exception in exceptions)
        {
            if (exception.ExceptionType == ExceptionType.DAY_OFF)
            {
                // Doctor is off for the entire day
                return new List<AppointmentTime>();
            }

            if (exception.AppointmentTime.HasValue && exception.AppointmentTime.Value != 0)
            {
                ProcessSlotException(availableSlots, exception);
            }
        }

        return availableSlots;
    }

    private static void ProcessSlotException(List<AppointmentTime> availableSlots, DoctorScheduleExceptionEntity exception)
    {
        if (!exception.IsAvailable)
        {
            // Slot is not available - remove it regardless of exception type
            availableSlots.Remove(exception.AppointmentTime!.Value);
        }
        else if (exception.ExceptionType == ExceptionType.UNBLOCK_SLOT)
        {
            // Slot is explicitly made available - add it if not already present
            if (!availableSlots.Contains(exception.AppointmentTime!.Value))
            {
                availableSlots.Add(exception.AppointmentTime.Value);
            }
        }
    }

    private async Task<List<AppointmentTime>> FilterSlotsByService(List<AppointmentTime> availableSlots, Guid serviceId)
    {
        var serviceSchedules = await GetServiceSchedulesAsync(serviceId);
        var serviceSlots = serviceSchedules
            .SelectMany(x => GetAppointmentTimesForPatterns(x.SchedulePatterns))
            .ToHashSet();

        // Only return slots that are available for both doctor and service
        return availableSlots
            .Where(slot => serviceSlots.Contains(slot))
            .ToList();
    }

    private IEnumerable<AppointmentTime> GetAppointmentTimesForPatterns(List<SchedulePatterns> patterns)
    {
        return patterns.SelectMany(GetAppointmentTimesForPattern)
                      .Distinct()
                      .OrderBy(slot => (int)slot);
    }

    private static IEnumerable<AppointmentTime> GetAppointmentTimesForPattern(SchedulePatterns pattern)
    {
        return pattern switch
        {
            SchedulePatterns.MORNING => GetMorningSlots(),
            SchedulePatterns.AFTERNOON => GetAfternoonSlots(),
            SchedulePatterns.EVENING => GetEveningSlots(),
            SchedulePatterns.FULL_DAY => GetMorningSlots().Concat(GetAfternoonSlots()).Concat(GetEveningSlots()),
            _ => Enumerable.Empty<AppointmentTime>()
        };
    }

    private static IEnumerable<AppointmentTime> GetMorningSlots()
    {
        // Morning slots: 8:00 AM to 12:00 PM (30-minute intervals)
        return new[]
        {
            AppointmentTime.AT_08_00_08_30, AppointmentTime.AT_08_30_09_00,
            AppointmentTime.AT_09_00_09_30, AppointmentTime.AT_09_30_10_00,
            AppointmentTime.AT_10_00_10_30, AppointmentTime.AT_10_30_11_00,
            AppointmentTime.AT_11_00_11_30, AppointmentTime.AT_11_30_12_00
        };
    }

    private static IEnumerable<AppointmentTime> GetAfternoonSlots()
    {
        // Afternoon slots: 1:00 PM to 5:00 PM (30-minute intervals)
        return new[]
        {
            AppointmentTime.AT_13_00_13_30, AppointmentTime.AT_13_30_14_00,
            AppointmentTime.AT_14_00_14_30, AppointmentTime.AT_14_30_15_00,
            AppointmentTime.AT_15_00_15_30, AppointmentTime.AT_15_30_16_00,
            AppointmentTime.AT_16_00_16_30, AppointmentTime.AT_16_30_17_00
        };
    }

    private static IEnumerable<AppointmentTime> GetEveningSlots()
    {
        // Evening slots: 5:00 PM to 9:00 PM (30-minute intervals)
        return new[]
        {
            AppointmentTime.AT_17_00_17_30, AppointmentTime.AT_17_30_18_00,
            AppointmentTime.AT_18_00_18_30, AppointmentTime.AT_18_30_19_00,
            AppointmentTime.AT_19_00_19_30, AppointmentTime.AT_19_30_20_00,
            AppointmentTime.AT_20_00_20_30, AppointmentTime.AT_20_30_21_00
        };
    }

    #endregion

    #region ServiceMedicalDailySchedule operations

    public async Task<ServiceMedicalDailyScheduleEntity?> GetServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date)
    {
        return await _context.ServiceMedicalDailySchedules
            .FirstOrDefaultAsync(x => x.ServiceMedicalId == serviceMedicalId && x.ScheduleDate == date);
    }

    public async Task<IEnumerable<ServiceMedicalDailyScheduleEntity>> GetServiceMedicalScheduleRangeAsync(Guid serviceMedicalId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.ServiceMedicalDailySchedules
            .Where(x => x.ServiceMedicalId == serviceMedicalId && x.ScheduleDate >= startDate && x.ScheduleDate <= endDate)
            .OrderBy(x => x.ScheduleDate)
            .ToListAsync();
    }

    public async Task<ServiceMedicalDailyScheduleEntity> CreateOrUpdateServiceMedicalDailyScheduleAsync(ServiceMedicalDailyScheduleEntity schedule)
    {
        var existing = await _context.ServiceMedicalDailySchedules
            .FirstOrDefaultAsync(x => x.ServiceMedicalId == schedule.ServiceMedicalId && x.ScheduleDate == schedule.ScheduleDate);

        if (existing != null)
        {
            existing.SchedulePatterns = schedule.SchedulePatterns;
            existing.UpdatedAt = DateTime.UtcNow;
            _context.ServiceMedicalDailySchedules.Update(existing);
        }
        else
        {
            _context.ServiceMedicalDailySchedules.Add(schedule);
        }

        await _context.SaveChangesAsync();
        return existing ?? schedule;
    }

    public async Task DeleteServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date)
    {
        var schedule = await _context.ServiceMedicalDailySchedules
            .FirstOrDefaultAsync(x => x.ServiceMedicalId == serviceMedicalId && x.ScheduleDate == date);

        if (schedule != null)
        {
            _context.ServiceMedicalDailySchedules.Remove(schedule);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region ServiceMedicalScheduleException operations

    public async Task<IEnumerable<ServiceMedicalScheduleExceptionEntity>> GetServiceMedicalExceptionsAsync(Guid serviceMedicalId, DateOnly date)
    {
        return await _context.ServiceMedicalScheduleExceptions
            .Where(x => x.ServiceMedicalId == serviceMedicalId && x.ExceptionDate == date)
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceMedicalScheduleExceptionEntity>> GetServiceMedicalExceptionsRangeAsync(Guid serviceMedicalId, DateOnly startDate, DateOnly endDate)
    {
        return await _context.ServiceMedicalScheduleExceptions
            .Where(x => x.ServiceMedicalId == serviceMedicalId && x.ExceptionDate >= startDate && x.ExceptionDate <= endDate)
            .OrderBy(x => x.ExceptionDate)
            .ToListAsync();
    }

    public async Task<ServiceMedicalScheduleExceptionEntity> CreateServiceMedicalScheduleExceptionAsync(ServiceMedicalScheduleExceptionEntity exception)
    {
        _context.ServiceMedicalScheduleExceptions.Add(exception);
        await _context.SaveChangesAsync();
        return exception;
    }

    public async Task DeleteServiceMedicalScheduleExceptionAsync(Guid id)
    {
        var exception = await _context.ServiceMedicalScheduleExceptions.FindAsync(id);
        if (exception != null)
        {
            _context.ServiceMedicalScheduleExceptions.Remove(exception);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region ServiceMedical Available slots operations

    public async Task<IEnumerable<AppointmentTime>> GetServiceMedicalAvailableSlotsAsync(Guid serviceMedicalId, DateOnly date)
    {
        // Get service medical's schedule for the day
        var serviceMedicalSchedule = await GetServiceMedicalDailyScheduleAsync(serviceMedicalId, date);
        if (serviceMedicalSchedule == null)
        {
            return new List<AppointmentTime>();
        }

        // Get all slots from the patterns based on enum collection
        var availableSlots = GetAppointmentTimesForPatterns(serviceMedicalSchedule.SchedulePatterns).ToList();

        // Get service medical's exceptions for the day
        var exceptions = await GetServiceMedicalExceptionsAsync(serviceMedicalId, date);

        // Apply exceptions to filter available slots
        availableSlots = ApplyServiceMedicalExceptionsToSlots(availableSlots, exceptions);

        return availableSlots.OrderBy(x => (int)x);
    }

    private static List<AppointmentTime> ApplyServiceMedicalExceptionsToSlots(List<AppointmentTime> availableSlots, IEnumerable<ServiceMedicalScheduleExceptionEntity> exceptions)
    {
        foreach (var exception in exceptions)
        {
            if (exception.ExceptionType == ExceptionType.DAY_OFF)
            {
                // Service is off for the entire day
                return new List<AppointmentTime>();
            }

            if (exception.AppointmentTime.HasValue && exception.AppointmentTime.Value != 0)
            {
                ProcessServiceMedicalSlotException(availableSlots, exception);
            }
        }

        return availableSlots;
    }

    private static void ProcessServiceMedicalSlotException(List<AppointmentTime> availableSlots, ServiceMedicalScheduleExceptionEntity exception)
    {
        if (!exception.IsAvailable)
        {
            // Slot is not available - remove it regardless of exception type
            availableSlots.Remove(exception.AppointmentTime!.Value);
        }
        else if (exception.ExceptionType == ExceptionType.UNBLOCK_SLOT)
        {
            // Slot is explicitly made available - add it if not already present
            if (!availableSlots.Contains(exception.AppointmentTime!.Value))
            {
                availableSlots.Add(exception.AppointmentTime.Value);
            }
        }
    }

    #endregion
}