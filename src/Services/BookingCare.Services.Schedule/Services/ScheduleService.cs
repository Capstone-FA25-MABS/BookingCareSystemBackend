using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Repositories;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Enums;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// Service implementation for schedule operations with Redis caching
/// </summary>
public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IScheduleRepository repository,
        ICacheService cacheService,
        ILogger<ScheduleService> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region DoctorDailySchedule operations

    public async Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(long doctorId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorDailySchedule, doctorId, date.ToString("yyyy-MM-dd"));

        var cached = await _cacheService.GetAsync<DoctorDailyScheduleDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved doctor {DoctorId} schedule for {Date} from cache", doctorId, date);
            return cached;
        }

        var entity = await _repository.GetDoctorDailyScheduleAsync(doctorId, date);
        if (entity == null) return null;

        var dto = MapToDto(entity);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dto;
    }

    public async Task<IEnumerable<DoctorDailyScheduleDto>> GetDoctorScheduleRangeAsync(GetDoctorScheduleRequest request)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorScheduleRange,
            request.DoctorId,
            request.StartDate.ToString("yyyy-MM-dd"),
            request.EndDate.ToString("yyyy-MM-dd"));

        var cached = await _cacheService.GetAsync<IEnumerable<DoctorDailyScheduleDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved doctor {DoctorId} schedule range from cache", request.DoctorId);
            return cached;
        }

        var entities = await _repository.GetDoctorScheduleRangeAsync(request.DoctorId, request.StartDate, request.EndDate);
        var dtos = entities.Select(MapToDto).ToList();

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<DoctorDailyScheduleDto> CreateOrUpdateDoctorDailyScheduleAsync(CreateDoctorDailyScheduleRequest request)
    {
        var entity = new DoctorDailyScheduleEntity
        {
            DoctorId = request.DoctorId,
            ScheduleDate = request.ScheduleDate,
            SchedulePattern = request.SchedulePattern
        };

        var created = await _repository.CreateOrUpdateDoctorDailyScheduleAsync(entity);

        // Invalidate related caches
        var dailyCacheKey = CacheKeys.Format(CacheKeys.DoctorDailySchedule, request.DoctorId, request.ScheduleDate.ToString("yyyy-MM-dd"));
        await _cacheService.RemoveAsync(dailyCacheKey);

        // Invalidate available slots cache
        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, request.DoctorId, request.ScheduleDate.ToString("yyyy-MM-dd"), "*");
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);

        return MapToDto(created);
    }

    public async Task DeleteDoctorDailyScheduleAsync(long doctorId, DateOnly date)
    {
        await _repository.DeleteDoctorDailyScheduleAsync(doctorId, date);

        // Invalidate related caches
        var dailyCacheKey = CacheKeys.Format(CacheKeys.DoctorDailySchedule, doctorId, date.ToString("yyyy-MM-dd"));
        await _cacheService.RemoveAsync(dailyCacheKey);

        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, doctorId, date.ToString("yyyy-MM-dd"), "*");
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);
    }

    #endregion

    #region DoctorScheduleException operations

    public async Task<IEnumerable<DoctorScheduleExceptionDto>> GetDoctorExceptionsAsync(long doctorId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorExceptions, doctorId, date.ToString("yyyy-MM-dd"));

        var cached = await _cacheService.GetAsync<IEnumerable<DoctorScheduleExceptionDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved doctor {DoctorId} exceptions for {Date} from cache", doctorId, date);
            return cached;
        }

        var entities = await _repository.GetDoctorExceptionsAsync(doctorId, date);
        var dtos = entities.Select(MapToDto).ToList();

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<DoctorScheduleExceptionDto> CreateDoctorScheduleExceptionAsync(CreateDoctorScheduleExceptionRequest request)
    {
        var entity = new DoctorScheduleExceptionEntity
        {
            DoctorId = request.DoctorId,
            ExceptionDate = request.ExceptionDate,
            AppointmentTime = request.AppointmentTime,
            ExceptionType = request.ExceptionType,
            IsAvailable = request.IsAvailable,
            Reason = request.Reason
        };

        var created = await _repository.CreateDoctorScheduleExceptionAsync(entity);

        // Invalidate related caches
        var exceptionsCacheKey = CacheKeys.Format(CacheKeys.DoctorExceptions, request.DoctorId, request.ExceptionDate.ToString("yyyy-MM-dd"));
        await _cacheService.RemoveAsync(exceptionsCacheKey);

        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, request.DoctorId, request.ExceptionDate.ToString("yyyy-MM-dd"), "*");
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);

        return MapToDto(created);
    }

    public async Task DeleteDoctorScheduleExceptionAsync(long id)
    {
        await _repository.DeleteDoctorScheduleExceptionAsync(id);

        // Note: We would need to know the doctor and date to invalidate specific cache keys
        // For now, we'll use pattern-based invalidation
        await _cacheService.RemoveByPatternAsync(CacheKeys.SchedulePattern);
    }

    #endregion

    #region ClinicException operations

    public async Task<IEnumerable<ClinicExceptionDto>> GetClinicExceptionsAsync(long clinicId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ClinicExceptions, clinicId, date.ToString("yyyy-MM-dd"));

        var cached = await _cacheService.GetAsync<IEnumerable<ClinicExceptionDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved clinic {ClinicId} exceptions for {Date} from cache", clinicId, date);
            return cached;
        }

        var entities = await _repository.GetClinicExceptionsAsync(clinicId, date);
        var dtos = entities.Select(MapToDto).ToList();

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<ClinicExceptionDto> CreateClinicExceptionAsync(CreateClinicExceptionRequest request)
    {
        var entity = new ClinicExceptionEntity
        {
            ClinicId = request.ClinicId,
            ExceptionDate = request.ExceptionDate,
            Reason = request.Reason
        };

        var created = await _repository.CreateClinicExceptionAsync(entity);

        // Invalidate cache
        var cacheKey = CacheKeys.Format(CacheKeys.ClinicExceptions, request.ClinicId, request.ExceptionDate.ToString("yyyy-MM-dd"));
        await _cacheService.RemoveAsync(cacheKey);

        return MapToDto(created);
    }

    public async Task DeleteClinicExceptionAsync(long id)
    {
        await _repository.DeleteClinicExceptionAsync(id);

        // Pattern-based invalidation
        await _cacheService.RemoveByPatternAsync("clinic_exceptions:*");
    }

    #endregion

    #region ServiceSchedule operations

    public async Task<IEnumerable<ServiceScheduleDto>> GetServiceSchedulesAsync(long serviceId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ServiceSchedules, serviceId);

        var cached = await _cacheService.GetAsync<IEnumerable<ServiceScheduleDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved service {ServiceId} schedules from cache", serviceId);
            return cached;
        }

        var entities = await _repository.GetServiceSchedulesAsync(serviceId);
        var dtos = entities.Select(MapToDto).ToList();

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.MediumCacheExpiration));

        return dtos;
    }

    public async Task<ServiceScheduleDto> CreateServiceScheduleAsync(CreateServiceScheduleRequest request)
    {
        var entity = new ServiceScheduleEntity
        {
            ServiceId = request.ServiceId,
            SchedulePattern = request.SchedulePattern,
            ClinicId = request.ClinicId
        };

        var created = await _repository.CreateServiceScheduleAsync(entity);

        // Invalidate cache
        var cacheKey = CacheKeys.Format(CacheKeys.ServiceSchedules, request.ServiceId);
        await _cacheService.RemoveAsync(cacheKey);

        return MapToDto(created);
    }

    public async Task DeleteServiceScheduleAsync(long id)
    {
        await _repository.DeleteServiceScheduleAsync(id);

        // Pattern-based invalidation
        await _cacheService.RemoveByPatternAsync("service_schedules:*");
    }

    #endregion

    #region Available slots operations

    public async Task<IEnumerable<AppointmentTimeDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request)
    {
        var serviceIdStr = request.ServiceId?.ToString() ?? "null";
        var cacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, request.DoctorId, request.Date.ToString("yyyy-MM-dd"), serviceIdStr);

        var cached = await _cacheService.GetAsync<IEnumerable<AppointmentTimeDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved available slots for doctor {DoctorId} on {Date} from cache", request.DoctorId, request.Date);
            return cached;
        }

        var entities = await _repository.GetAvailableSlotsAsync(request.DoctorId, request.Date, request.ServiceId);
        var dtos = entities.Select(ConvertEnumToDto).ToList();

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    #endregion

    #region Mapping methods

    private static AppointmentTimeDto ConvertEnumToDto(AppointmentTime appointmentTime)
    {
        var (startTime, endTime) = GetTimeStringsFromEnum(appointmentTime);
        return new AppointmentTimeDto
        {
            Id = (int)appointmentTime,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    private static (string startTime, string endTime) GetTimeStringsFromEnum(AppointmentTime appointmentTime)
    {
        return appointmentTime switch
        {
            AppointmentTime.AT_08_00_08_30 => ("08:00", "08:30"),
            AppointmentTime.AT_08_30_09_00 => ("08:30", "09:00"),
            AppointmentTime.AT_09_00_09_30 => ("09:00", "09:30"),
            AppointmentTime.AT_09_30_10_00 => ("09:30", "10:00"),
            AppointmentTime.AT_10_00_10_30 => ("10:00", "10:30"),
            AppointmentTime.AT_10_30_11_00 => ("10:30", "11:00"),
            AppointmentTime.AT_11_00_11_30 => ("11:00", "11:30"),
            AppointmentTime.AT_11_30_12_00 => ("11:30", "12:00"),
            AppointmentTime.AT_13_00_13_30 => ("13:00", "13:30"),
            AppointmentTime.AT_13_30_14_00 => ("13:30", "14:00"),
            AppointmentTime.AT_14_00_14_30 => ("14:00", "14:30"),
            AppointmentTime.AT_14_30_15_00 => ("14:30", "15:00"),
            AppointmentTime.AT_15_00_15_30 => ("15:00", "15:30"),
            AppointmentTime.AT_15_30_16_00 => ("15:30", "16:00"),
            AppointmentTime.AT_16_00_16_30 => ("16:00", "16:30"),
            AppointmentTime.AT_16_30_17_00 => ("16:30", "17:00"),
            AppointmentTime.AT_08_00_09_00 => ("08:00", "09:00"),
            AppointmentTime.AT_09_00_10_00 => ("09:00", "10:00"),
            AppointmentTime.AT_10_00_11_00 => ("10:00", "11:00"),
            AppointmentTime.AT_11_00_12_00 => ("11:00", "12:00"),
            AppointmentTime.AT_13_00_14_00 => ("13:00", "14:00"),
            AppointmentTime.AT_14_00_15_00 => ("14:00", "15:00"),
            AppointmentTime.AT_15_00_16_00 => ("15:00", "16:00"),
            AppointmentTime.AT_16_00_17_00 => ("16:00", "17:00"),
            _ => ("Unknown", "Unknown")
        };
    }



    private static DoctorDailyScheduleDto MapToDto(DoctorDailyScheduleEntity entity)
    {
        return new DoctorDailyScheduleDto
        {
            Id = entity.Id,
            DoctorId = entity.DoctorId,
            ScheduleDate = entity.ScheduleDate,
            SchedulePattern = entity.SchedulePattern,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static DoctorScheduleExceptionDto MapToDto(DoctorScheduleExceptionEntity entity)
    {
        return new DoctorScheduleExceptionDto
        {
            Id = entity.Id,
            DoctorId = entity.DoctorId,
            ExceptionDate = entity.ExceptionDate,
            AppointmentTime = entity.AppointmentTime,
            ExceptionType = entity.ExceptionType.ToString(),
            IsAvailable = entity.IsAvailable,
            Reason = entity.Reason,
            CreatedAt = entity.CreatedAt
        };
    }

    private static ClinicExceptionDto MapToDto(ClinicExceptionEntity entity)
    {
        return new ClinicExceptionDto
        {
            Id = entity.Id,
            ClinicId = entity.ClinicId,
            ExceptionDate = entity.ExceptionDate,
            Reason = entity.Reason
        };
    }

    private static ServiceScheduleDto MapToDto(ServiceScheduleEntity entity)
    {
        return new ServiceScheduleDto
        {
            Id = entity.Id,
            ServiceId = entity.ServiceId,
            SchedulePattern = entity.SchedulePattern,
            ClinicId = entity.ClinicId,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    #endregion
}