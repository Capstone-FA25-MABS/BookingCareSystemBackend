using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Repositories;
using BookingCare.Services.Schedule.Exceptions;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Shared.Common.Enums;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.ServiceMedical.Protos;
using BookingCare.Shared.Common.Exceptions.Domain;
using AutoMapper;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// Service implementation for schedule operations with Redis caching
/// </summary>
public class ScheduleService : IScheduleService
{
    private const string DateFormat = "yyyy-MM-dd";
    private readonly IScheduleRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ScheduleService> _logger;
    private readonly DoctorService.DoctorServiceClient _doctorClient;
    private readonly ServiceMedicalService.ServiceMedicalServiceClient _serviceMedicalClient;
    private readonly BookingCare.Services.Appointment.Protos.AppointmentService.AppointmentServiceClient _appointmentClient;
    private readonly IMapper _mapper;

    public ScheduleService(
        IScheduleRepository repository,
        ICacheService cacheService,
        ILogger<ScheduleService> logger,
        DoctorService.DoctorServiceClient doctorClient,
        ServiceMedicalService.ServiceMedicalServiceClient serviceMedicalClient,
        BookingCare.Services.Appointment.Protos.AppointmentService.AppointmentServiceClient appointmentClient,
        IMapper mapper)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
        _doctorClient = doctorClient;
        _serviceMedicalClient = serviceMedicalClient;
        _appointmentClient = appointmentClient;
        _mapper = mapper;
    }

    #region DoctorDailySchedule operations

    public async Task<DoctorDailyScheduleDto?> GetDoctorDailyScheduleAsync(Guid doctorId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorDailySchedule, doctorId, date.ToString(DateFormat));

        var cached = await _cacheService.GetAsync<DoctorDailyScheduleDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved doctor {DoctorId} schedule for {Date} from cache", doctorId, date);
            return cached;
        }

        var entity = await _repository.GetDoctorDailyScheduleAsync(doctorId, date);
        if (entity == null) return null;

        var dto = _mapper.Map<DoctorDailyScheduleDto>(entity);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dto;
    }

    public async Task<IEnumerable<DoctorDailyScheduleDto>> GetDoctorScheduleRangeAsync(GetDoctorScheduleRequest request)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorScheduleRange,
            request.DoctorId,
            request.StartDate.ToString(DateFormat),
            request.EndDate.ToString(DateFormat));

        var cached = await _cacheService.GetAsync<IEnumerable<DoctorDailyScheduleDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved doctor {DoctorId} schedule range from cache", request.DoctorId);
            return cached;
        }

        var entities = await _repository.GetDoctorScheduleRangeAsync(request.DoctorId, request.StartDate, request.EndDate);
        var dtos = _mapper.Map<List<DoctorDailyScheduleDto>>(entities);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<DoctorDailyScheduleDto> CreateOrUpdateDoctorDailyScheduleAsync(CreateDoctorDailyScheduleRequest request)
    {
        // Validate doctor exists and is active
        var isDoctorValid = await ValidateDoctorAsync(request.DoctorId);
        if (!isDoctorValid)
        {
            _logger.LogWarning("Invalid or inactive doctor {DoctorId} attempted to create schedule", request.DoctorId);
            throw new BookingCare.Shared.Common.Exceptions.Domain.DoctorExceptions.DoctorNotFoundException(request.DoctorId);
        }

        var entity = _mapper.Map<DoctorDailyScheduleEntity>(request);

        var created = await _repository.CreateOrUpdateDoctorDailyScheduleAsync(entity);

        // Invalidate related caches
        var dailyCacheKey = CacheKeys.Format(CacheKeys.DoctorDailySchedule, request.DoctorId, request.ScheduleDate.ToString(DateFormat));
        await _cacheService.RemoveAsync(dailyCacheKey);

        // Invalidate available slots cache
        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, request.DoctorId, request.ScheduleDate.ToString(DateFormat), "*");
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);

        return _mapper.Map<DoctorDailyScheduleDto>(created);
    }

    public async Task DeleteDoctorDailyScheduleAsync(Guid doctorId, DateOnly date)
    {
        await _repository.DeleteDoctorDailyScheduleAsync(doctorId, date);

        // Invalidate related caches
        var dailyCacheKey = CacheKeys.Format(CacheKeys.DoctorDailySchedule, doctorId, date.ToString(DateFormat));
        await _cacheService.RemoveAsync(dailyCacheKey);

        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, doctorId, date.ToString(DateFormat), "*");
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);
    }

    #endregion

    #region DoctorScheduleException operations

    public async Task<IEnumerable<DoctorScheduleExceptionDto>> GetDoctorExceptionsAsync(Guid doctorId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.DoctorExceptions, doctorId, date.ToString(DateFormat));

        var cached = await _cacheService.GetAsync<IEnumerable<DoctorScheduleExceptionDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved doctor {DoctorId} exceptions for {Date} from cache", doctorId, date);
            return cached;
        }

        var entities = await _repository.GetDoctorExceptionsAsync(doctorId, date);
        var dtos = _mapper.Map<List<DoctorScheduleExceptionDto>>(entities);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<List<DoctorScheduleExceptionDto>> CreateDoctorScheduleExceptionAsync(CreateDoctorScheduleExceptionRequest request)
    {
        var createdExceptions = new List<DoctorScheduleExceptionEntity>();

        // Handle multiple appointment times or single day-off exception
        if (request.AppointmentTimes == null || request.AppointmentTimes.Count == 0)
        {
            // Full day off - create single exception with null appointment time
            var dayOffEntity = _mapper.Map<DoctorScheduleExceptionEntity>(request);
            dayOffEntity.AppointmentTime = null;

            var created = await _repository.CreateDoctorScheduleExceptionAsync(dayOffEntity);
            createdExceptions.Add(created);
        }
        else
        {
            // Create exception for each appointment time
            foreach (var appointmentTime in request.AppointmentTimes)
            {
                var entity = _mapper.Map<DoctorScheduleExceptionEntity>(request);
                entity.AppointmentTime = appointmentTime;

                var created = await _repository.CreateDoctorScheduleExceptionAsync(entity);
                createdExceptions.Add(created);
            }
        }

        // Invalidate related caches
        var exceptionsCacheKey = CacheKeys.Format(CacheKeys.DoctorExceptions, request.DoctorId, request.ExceptionDate.ToString(DateFormat));
        await _cacheService.RemoveAsync(exceptionsCacheKey);

        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, request.DoctorId, request.ExceptionDate.ToString(DateFormat), "*");
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);

        return createdExceptions.Select(e => _mapper.Map<DoctorScheduleExceptionDto>(e)).ToList();
    }

    public async Task DeleteDoctorScheduleExceptionAsync(Guid id)
    {
        await _repository.DeleteDoctorScheduleExceptionAsync(id);

        // Note: We would need to know the doctor and date to invalidate specific cache keys
        // For now, we'll use pattern-based invalidation
        await _cacheService.RemoveByPatternAsync(CacheKeys.SchedulePattern);
    }

    #endregion

    #region ClinicException operations

    public async Task<IEnumerable<ClinicExceptionDto>> GetClinicExceptionsAsync(Guid clinicId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ClinicExceptions, clinicId, date.ToString(DateFormat));

        var cached = await _cacheService.GetAsync<IEnumerable<ClinicExceptionDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved clinic {ClinicId} exceptions for {Date} from cache", clinicId, date);
            return cached;
        }

        var entities = await _repository.GetClinicExceptionsAsync(clinicId, date);
        var dtos = _mapper.Map<List<ClinicExceptionDto>>(entities);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<ClinicExceptionDto> CreateClinicExceptionAsync(CreateClinicExceptionRequest request)
    {
        var entity = _mapper.Map<ClinicExceptionEntity>(request);

        var created = await _repository.CreateClinicExceptionAsync(entity);

        // Invalidate cache
        var cacheKey = CacheKeys.Format(CacheKeys.ClinicExceptions, request.ClinicId, request.ExceptionDate.ToString(DateFormat));
        await _cacheService.RemoveAsync(cacheKey);

        return _mapper.Map<ClinicExceptionDto>(created);
    }

    public async Task DeleteClinicExceptionAsync(Guid id)
    {
        await _repository.DeleteClinicExceptionAsync(id);

        // Pattern-based invalidation
        await _cacheService.RemoveByPatternAsync("clinic_exceptions:*");
    }

    #endregion

    #region ServiceSchedule operations

    public async Task<IEnumerable<ServiceScheduleDto>> GetServiceSchedulesAsync(Guid serviceId)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ServiceSchedules, serviceId);

        var cached = await _cacheService.GetAsync<IEnumerable<ServiceScheduleDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved service {ServiceId} schedules from cache", serviceId);
            return cached;
        }

        var entities = await _repository.GetServiceSchedulesAsync(serviceId);
        var dtos = _mapper.Map<List<ServiceScheduleDto>>(entities);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.MediumCacheExpiration));

        return dtos;
    }

    public async Task<ServiceScheduleDto> CreateServiceScheduleAsync(CreateServiceScheduleRequest request)
    {
        var entity = _mapper.Map<ServiceScheduleEntity>(request);

        var created = await _repository.CreateServiceScheduleAsync(entity);

        // Invalidate cache
        var cacheKey = CacheKeys.Format(CacheKeys.ServiceSchedules, request.ServiceId);
        await _cacheService.RemoveAsync(cacheKey);

        return _mapper.Map<ServiceScheduleDto>(created);
    }

    public async Task DeleteServiceScheduleAsync(Guid id)
    {
        await _repository.DeleteServiceScheduleAsync(id);

        // Pattern-based invalidation
        await _cacheService.RemoveByPatternAsync("service_schedules:*");
    }

    #endregion

    #region Available slots operations

    public async Task<IEnumerable<AppointmentTimeDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request)
    {
        // Validate doctor exists and is active
        var isDoctorValid = await ValidateDoctorAsync(request.DoctorId);
        if (!isDoctorValid)
        {
            _logger.LogWarning("Invalid or inactive doctor {DoctorId} attempted to get available slots", request.DoctorId);
            throw new BookingCare.Shared.Common.Exceptions.Domain.DoctorExceptions.DoctorNotAvailableException(request.DoctorId, DateTime.UtcNow);
        }

        // Validate service if provided
        if (request.ServiceId.HasValue)
        {
            var isServiceValid = await ValidateServiceMedicalAsync(request.ServiceId.Value);
            if (!isServiceValid)
            {
                _logger.LogWarning("Invalid or inactive service {ServiceId} for available slots request", request.ServiceId);
                throw ServiceNotAvailableException.WithId(request.ServiceId.Value);
            }
        }

        var serviceIdStr = request.ServiceId?.ToString() ?? "null";
        var cacheKey = CacheKeys.Format(CacheKeys.AvailableSlots, request.DoctorId, request.Date.ToString(DateFormat), serviceIdStr);

        var cached = await _cacheService.GetAsync<IEnumerable<AppointmentTimeDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved available slots for doctor {DoctorId} on {Date} from cache", request.DoctorId, request.Date);
            return cached;
        }

        // Get all potential available slots from schedule
        var entities = await _repository.GetAvailableSlotsAsync(request.DoctorId, request.Date, request.ServiceId);
            var allSlots = entities.Select(BookingCare.Services.Schedule.Utilities.AppointmentTimeHelper.ConvertEnumToDto).ToList();

        // Check which slots are already booked via gRPC call to Appointment service
        try
        {
            var checkBookedRequest = new BookingCare.Services.Appointment.Protos.CheckBookedSlotsRequest
            {
                DoctorId = request.DoctorId.ToString(),
                AppointmentDate = request.Date.ToString(DateFormat)
            };

            var bookedSlotsResponse = await _appointmentClient.CheckBookedSlotsAsync(checkBookedRequest);
            var bookedTimeIds = new HashSet<int>(bookedSlotsResponse.BookedAppointmentTimeIds);

            _logger.LogInformation("Doctor {DoctorId} on {Date}: Found {TotalSlots} potential slots, {BookedSlots} already booked",
                request.DoctorId, request.Date, allSlots.Count, bookedTimeIds.Count);

            // Filter out booked slots - only return slots that are NOT booked
            // We need to match AppointmentTimeDto.Id with AppointmentTime enum values
            var availableSlots = allSlots.Where(slot =>
            {
                // Extract the AppointmentTime enum value from the deterministic GUID
                var slotBytes = slot.Id.ToByteArray();
                var enumValue = BitConverter.ToInt32(slotBytes, 0);
                return !bookedTimeIds.Contains(enumValue);
            }).ToList();

            _logger.LogInformation("Returning {AvailableCount} available slots after filtering booked slots",
                availableSlots.Count);

            await _cacheService.SetAsync(cacheKey, availableSlots, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

            return availableSlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking booked slots for doctor {DoctorId} on {Date}. Returning all slots without filtering.",
                request.DoctorId, request.Date);

            // Fallback: return all slots if appointment service is unavailable
            await _cacheService.SetAsync(cacheKey, allSlots, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));
            return allSlots;
        }
    }

    #endregion

    #region Mapping methods

    // Mapping and helpers for AppointmentTime were extracted to AppointmentTimeHelper to avoid duplication across mapping and service

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates if a doctor exists and is active using gRPC call to Doctor service
    /// </summary>
    private async Task<bool> ValidateDoctorAsync(Guid doctorId)
    {
        try
        {
            var request = new GetDoctorRequest
            {
                Id = doctorId.ToString()
            };

            var response = await _doctorClient.GetDoctorAsync(request);

            if (response != null && !string.IsNullOrEmpty(response.Id))
            {
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating doctor {DoctorId}", doctorId);
            return false;
        }
    }

    /// <summary>
    /// Validates if a medical service exists and is active using gRPC call to ServiceMedical service
    /// </summary>
    private async Task<bool> ValidateServiceMedicalAsync(Guid serviceId)
    {
        try
        {
            var request = new ValidateServiceMedicalRequest
            {
                Id = serviceId.ToString()
            };

            var response = await _serviceMedicalClient.ValidateServiceMedicalAsync(request);

            return response != null && response.IsValid && response.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating service {ServiceId}", serviceId);
            return false;
        }
    }

    #endregion
}