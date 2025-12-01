using BookingCare.Services.Schedule.Models.DTOs;
using BookingCare.Services.Schedule.Models.Entities;
using BookingCare.Services.Schedule.Models.Requests;
using BookingCare.Services.Schedule.Repositories;
using BookingCare.Services.Schedule.Exceptions;
using BookingCare.Shared.Cache.Abstractions;
using BookingCare.Shared.Cache.Constants;
using BookingCare.Services.Doctor.Protos;
using BookingCare.Services.ServiceMedical.Protos;
using AutoMapper;
using BookingCare.Services.Schedule.Enums;
using BookingCare.Shared.Common.Enums;

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
    private readonly GrpcClients _grpcClients;
    private readonly IMapper _mapper;
    private readonly IHoldSlotService _holdSlotService;

    public ScheduleService(
        IScheduleRepository repository,
        ICacheService cacheService,
        ILogger<ScheduleService> logger,
        GrpcClients grpcClients,
        IMapper mapper,
        IHoldSlotService holdSlotService)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
        _grpcClients = grpcClients;
        _mapper = mapper;
        _holdSlotService = holdSlotService;
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

    public async Task<IEnumerable<AppointmentTimeDto>> GetAvailableSlotsAsync(GetAvailableSlotsRequest request, Guid? currentUserId = null)
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

        // Use cache for all users with short TTL (30s) to balance performance and real-time data
        // Held slots will be filtered after cache retrieval to ensure real-time availability
        var cached = await _cacheService.GetAsync<IEnumerable<AppointmentTimeDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved available slots for doctor {DoctorId} on {Date} from cache", request.DoctorId, request.Date);
            var cachedList = cached.ToList();

            // Always filter held slots in real-time, even from cache
            var heldSlots = await _holdSlotService.GetHeldSlotsAsync(request.DoctorId, request.Date, currentUserId ?? Guid.Empty);
            if (heldSlots.Any())
            {
                var heldTimeIds = new HashSet<int>(heldSlots.Select(hs => (int)hs));
                cachedList = cachedList.Where(slot =>
                {
                    var slotBytes = slot.Id.ToByteArray();
                    var enumValue = BitConverter.ToInt32(slotBytes, 0);
                    return !heldTimeIds.Contains(enumValue);
                }).ToList();

                _logger.LogInformation("Filtered {0} held slots from cached data for doctor {1}",
                    heldSlots.Count, request.DoctorId);
            }

            return cachedList;
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

            var bookedSlotsResponse = await _grpcClients.AppointmentClient.CheckBookedSlotsAsync(checkBookedRequest);
            var bookedTimeIds = new HashSet<int>(bookedSlotsResponse.BookedAppointmentTimeIds);

            // Filter out booked slots - only return slots that are NOT booked
            // We need to match AppointmentTimeDto.Id with AppointmentTime enum values
            var availableSlots = allSlots.Where(slot =>
            {
                // Extract the AppointmentTime enum value from the deterministic GUID
                var slotBytes = slot.Id.ToByteArray();
                var enumValue = BitConverter.ToInt32(slotBytes, 0);
                return !bookedTimeIds.Contains(enumValue);
            }).ToList();

            _logger.LogInformation("Doctor {DoctorId} on {Date}: Found {TotalSlots} potential slots, {BookedSlots} booked, {AvailableCount} available after filtering",
                request.DoctorId, request.Date, allSlots.Count, bookedTimeIds.Count, availableSlots.Count);

            // Always filter out held slots for all users to ensure real-time availability
            var heldSlots = await _holdSlotService.GetHeldSlotsAsync(request.DoctorId, request.Date, currentUserId ?? Guid.Empty);
            if (heldSlots.Any())
            {
                var heldTimeIds = new HashSet<int>(heldSlots.Select(hs => (int)hs));
                var slotsBeforeHeldFilter = availableSlots.Count;

                availableSlots = availableSlots.Where(slot =>
                {
                    var slotBytes = slot.Id.ToByteArray();
                    var enumValue = BitConverter.ToInt32(slotBytes, 0);
                    return !heldTimeIds.Contains(enumValue);
                }).ToList();

                _logger.LogInformation("Doctor {DoctorId} on {Date}: Filtered out {HeldSlots} held slots, returning {FinalCount} available slots (User: {UserId})",
                    request.DoctorId, request.Date, slotsBeforeHeldFilter - availableSlots.Count, availableSlots.Count,
                    currentUserId?.ToString() ?? "Anonymous");
            }

            // Cache for all users with short TTL (30 seconds) to balance performance and real-time data
            // Held slots will be filtered in real-time on each request
            await _cacheService.SetAsync(cacheKey, availableSlots, TimeSpan.FromSeconds(30));

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

            var response = await _grpcClients.DoctorClient.GetDoctorAsync(request);

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

            var response = await _grpcClients.ServiceMedicalClient.ValidateServiceMedicalAsync(request);

            return response != null && response.IsValid && response.IsActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating service {ServiceId}", serviceId);
            return false;
        }
    }

    #endregion

    #region ServiceMedicalDailySchedule operations

    public async Task<ServiceMedicalDailyScheduleDto?> GetServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalDailySchedule, serviceMedicalId, date.ToString(DateFormat));

        var cached = await _cacheService.GetAsync<ServiceMedicalDailyScheduleDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved service medical {ServiceMedicalId} schedule for {Date} from cache", serviceMedicalId, date);
            return cached;
        }

        var entity = await _repository.GetServiceMedicalDailyScheduleAsync(serviceMedicalId, date);
        if (entity == null) return null;

        var dto = _mapper.Map<ServiceMedicalDailyScheduleDto>(entity);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dto;
    }

    public async Task<IEnumerable<ServiceMedicalDailyScheduleDto>> GetServiceMedicalScheduleRangeAsync(GetServiceMedicalScheduleRequest request)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalScheduleRange,
            request.ServiceMedicalId,
            request.StartDate.ToString(DateFormat),
            request.EndDate.ToString(DateFormat));

        var cached = await _cacheService.GetAsync<IEnumerable<ServiceMedicalDailyScheduleDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved service medical {ServiceMedicalId} schedule range from cache", request.ServiceMedicalId);
            return cached;
        }

        var entities = await _repository.GetServiceMedicalScheduleRangeAsync(request.ServiceMedicalId, request.StartDate, request.EndDate);
        var dtos = _mapper.Map<List<ServiceMedicalDailyScheduleDto>>(entities);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<ServiceMedicalDailyScheduleDto> CreateOrUpdateServiceMedicalDailyScheduleAsync(CreateServiceMedicalDailyScheduleRequest request)
    {
        // Validate service medical exists and is active
        var isServiceValid = await ValidateServiceMedicalAsync(request.ServiceMedicalId);
        if (!isServiceValid)
        {
            _logger.LogWarning("Invalid or inactive service medical {ServiceMedicalId} attempted to create schedule", request.ServiceMedicalId);
            throw ServiceNotAvailableException.WithId(request.ServiceMedicalId);
        }

        var entity = _mapper.Map<ServiceMedicalDailyScheduleEntity>(request);

        var created = await _repository.CreateOrUpdateServiceMedicalDailyScheduleAsync(entity);

        // Invalidate related caches
        var dailyCacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalDailySchedule, request.ServiceMedicalId, request.ScheduleDate.ToString(DateFormat));
        await _cacheService.RemoveAsync(dailyCacheKey);

        // Invalidate available slots cache
        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalAvailableSlots, request.ServiceMedicalId, request.ScheduleDate.ToString(DateFormat));
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);

        return _mapper.Map<ServiceMedicalDailyScheduleDto>(created);
    }

    public async Task DeleteServiceMedicalDailyScheduleAsync(Guid serviceMedicalId, DateOnly date)
    {
        await _repository.DeleteServiceMedicalDailyScheduleAsync(serviceMedicalId, date);

        // Invalidate related caches
        var dailyCacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalDailySchedule, serviceMedicalId, date.ToString(DateFormat));
        await _cacheService.RemoveAsync(dailyCacheKey);

        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalAvailableSlots, serviceMedicalId, date.ToString(DateFormat));
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);
    }

    #endregion

    #region ServiceMedicalScheduleException operations

    public async Task<IEnumerable<ServiceMedicalScheduleExceptionDto>> GetServiceMedicalExceptionsAsync(Guid serviceMedicalId, DateOnly date)
    {
        var cacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalExceptions, serviceMedicalId, date.ToString(DateFormat));

        var cached = await _cacheService.GetAsync<IEnumerable<ServiceMedicalScheduleExceptionDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved service medical {ServiceMedicalId} exceptions for {Date} from cache", serviceMedicalId, date);
            return cached;
        }

        var entities = await _repository.GetServiceMedicalExceptionsAsync(serviceMedicalId, date);
        var dtos = _mapper.Map<List<ServiceMedicalScheduleExceptionDto>>(entities);

        await _cacheService.SetAsync(cacheKey, dtos, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));

        return dtos;
    }

    public async Task<List<ServiceMedicalScheduleExceptionDto>> CreateServiceMedicalScheduleExceptionAsync(CreateServiceMedicalScheduleExceptionRequest request)
    {
        // Validate service medical exists and is active
        var isServiceValid = await ValidateServiceMedicalAsync(request.ServiceMedicalId);
        if (!isServiceValid)
        {
            _logger.LogWarning("Invalid or inactive service medical {ServiceMedicalId} attempted to create exception", request.ServiceMedicalId);
            throw ServiceNotAvailableException.WithId(request.ServiceMedicalId);
        }

        var createdExceptions = new List<ServiceMedicalScheduleExceptionEntity>();

        // Handle multiple appointment times or single day-off exception
        if (request.AppointmentTimes == null || request.AppointmentTimes.Count == 0)
        {
            // Full day off - create single exception with null appointment time
            var dayOffEntity = _mapper.Map<ServiceMedicalScheduleExceptionEntity>(request);
            dayOffEntity.AppointmentTime = null;

            var created = await _repository.CreateServiceMedicalScheduleExceptionAsync(dayOffEntity);
            createdExceptions.Add(created);
        }
        else
        {
            // Create exception for each appointment time
            foreach (var appointmentTime in request.AppointmentTimes)
            {
                var entity = _mapper.Map<ServiceMedicalScheduleExceptionEntity>(request);
                entity.AppointmentTime = appointmentTime;

                var created = await _repository.CreateServiceMedicalScheduleExceptionAsync(entity);
                createdExceptions.Add(created);
            }
        }

        // Invalidate related caches
        var exceptionsCacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalExceptions, request.ServiceMedicalId, request.ExceptionDate.ToString(DateFormat));
        await _cacheService.RemoveAsync(exceptionsCacheKey);

        var availableSlotsCacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalAvailableSlots, request.ServiceMedicalId, request.ExceptionDate.ToString(DateFormat));
        await _cacheService.RemoveByPatternAsync(availableSlotsCacheKey);

        return createdExceptions.Select(e => _mapper.Map<ServiceMedicalScheduleExceptionDto>(e)).ToList();
    }

    public async Task DeleteServiceMedicalScheduleExceptionAsync(Guid id)
    {
        await _repository.DeleteServiceMedicalScheduleExceptionAsync(id);

        // Pattern-based invalidation
        await _cacheService.RemoveByPatternAsync("service_medical_exceptions:*");
    }

    #endregion

    #region ServiceMedical Available slots operations

    public async Task<IEnumerable<AppointmentTimeDto>> GetServiceMedicalAvailableSlotsAsync(GetServiceMedicalAvailableSlotsRequest request, Guid? currentUserId = null)
    {
        // Validate service medical exists and is active
        var isServiceValid = await ValidateServiceMedicalAsync(request.ServiceMedicalId);
        if (!isServiceValid)
        {
            _logger.LogWarning("Invalid or inactive service medical {ServiceMedicalId} attempted to get available slots", request.ServiceMedicalId);
            throw ServiceNotAvailableException.WithId(request.ServiceMedicalId);
        }

        var cacheKey = CacheKeys.Format(CacheKeys.ServiceMedicalAvailableSlots, request.ServiceMedicalId, request.Date.ToString(DateFormat));

        // Use cache with short TTL (30s) to balance performance and real-time data
        // Held slots will be filtered after cache retrieval to ensure real-time availability
        var cached = await _cacheService.GetAsync<IEnumerable<AppointmentTimeDto>>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved available slots for service medical {ServiceMedicalId} on {Date} from cache", request.ServiceMedicalId, request.Date);
            var cachedList = cached.ToList();

            // Always filter held slots in real-time, even from cache
            var heldSlots = await _holdSlotService.GetHeldSlotsAsync(request.ServiceMedicalId, HoldSlotTargetType.ServiceMedical, request.Date, currentUserId ?? Guid.Empty);
            if (heldSlots.Any())
            {
                var heldTimeIds = new HashSet<int>(heldSlots.Select(hs => (int)hs));
                cachedList = cachedList.Where(slot =>
                {
                    var slotBytes = slot.Id.ToByteArray();
                    var enumValue = BitConverter.ToInt32(slotBytes, 0);
                    return !heldTimeIds.Contains(enumValue);
                }).ToList();

                _logger.LogInformation("Filtered {0} held slots from cached data for service medical {1}",
                    heldSlots.Count, request.ServiceMedicalId);
            }

            return cachedList;
        }

        // Get all potential available slots from schedule
        var entities = await _repository.GetServiceMedicalAvailableSlotsAsync(request.ServiceMedicalId, request.Date);
        var allSlots = entities.Select(BookingCare.Services.Schedule.Utilities.AppointmentTimeHelper.ConvertEnumToDto).ToList();

        // Check which slots are already booked via gRPC call to Appointment service
        try
        {
            var checkBookedRequest = new BookingCare.Services.Appointment.Protos.CheckServiceBookedSlotsRequest
            {
                ServiceId = request.ServiceMedicalId.ToString(),
                AppointmentDate = request.Date.ToString(DateFormat)
            };

            var bookedSlotsResponse = await _grpcClients.AppointmentClient.CheckServiceBookedSlotsAsync(checkBookedRequest);
            var bookedTimeIds = new HashSet<int>(bookedSlotsResponse.BookedAppointmentTimeIds);

            // Filter out booked slots - only return slots that are NOT booked
            var availableSlots = allSlots.Where(slot =>
            {
                var slotBytes = slot.Id.ToByteArray();
                var enumValue = BitConverter.ToInt32(slotBytes, 0);
                return !bookedTimeIds.Contains(enumValue);
            }).ToList();

            _logger.LogInformation("Service medical {ServiceMedicalId} on {Date}: Found {TotalSlots} potential slots, {BookedSlots} booked, {AvailableCount} available after filtering",
                request.ServiceMedicalId, request.Date, allSlots.Count, bookedTimeIds.Count, availableSlots.Count);

            // Always filter out held slots for all users to ensure real-time availability
            var heldSlots = await _holdSlotService.GetHeldSlotsAsync(request.ServiceMedicalId, HoldSlotTargetType.ServiceMedical, request.Date, currentUserId ?? Guid.Empty);
            if (heldSlots.Any())
            {
                var heldTimeIds = new HashSet<int>(heldSlots.Select(hs => (int)hs));
                var slotsBeforeHeldFilter = availableSlots.Count;

                availableSlots = availableSlots.Where(slot =>
                {
                    var slotBytes = slot.Id.ToByteArray();
                    var enumValue = BitConverter.ToInt32(slotBytes, 0);
                    return !heldTimeIds.Contains(enumValue);
                }).ToList();

                _logger.LogInformation("Service medical {ServiceMedicalId} on {Date}: Filtered out {HeldSlots} held slots, returning {FinalCount} available slots (User: {UserId})",
                    request.ServiceMedicalId, request.Date, slotsBeforeHeldFilter - availableSlots.Count, availableSlots.Count,
                    currentUserId?.ToString() ?? "Anonymous");
            }

            // Cache for all users with short TTL (30 seconds) to balance performance and real-time data
            await _cacheService.SetAsync(cacheKey, availableSlots, TimeSpan.FromSeconds(30));

            return availableSlots;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking booked slots for service medical {ServiceMedicalId} on {Date}. Returning all slots without filtering.",
                request.ServiceMedicalId, request.Date);

            // Fallback: return all slots if appointment service is unavailable
            await _cacheService.SetAsync(cacheKey, allSlots, TimeSpan.FromMinutes(CacheKeys.ShortCacheExpiration));
            return allSlots;
        }
    }

    #endregion

    #region Specialty Available Slots operations

    /// <summary>
    /// Get aggregated available slots for a specialty (hospital assigns doctor mode)
    /// This aggregates availability across all doctors in the specialty
    /// </summary>
    public async Task<SpecialtyAvailableSlotsResponseDto> GetSpecialtyAvailableSlotsAsync(
        GetSpecialtyAvailableSlotsRequest request,
        Guid? currentUserId = null)
    {
        var dateStr = request.Date.ToString(DateFormat);
        var cacheKey = CacheKeys.Format(
            CacheKeys.SpecialtyAvailableSlots,
            request.HospitalId,
            request.SpecialtyId,
            dateStr,
            request.AppointmentType.ToString());

        // Try to get from cache first (short TTL: 1-2 minutes as confirmed)
        var cached = await _cacheService.GetAsync<SpecialtyAvailableSlotsResponseDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Retrieved specialty available slots from cache for hospital {HospitalId}, specialty {SpecialtyId} on {Date}",
                request.HospitalId, request.SpecialtyId, request.Date);

            // Subtract booked appointments in real-time (appointments may have been created after cache)
            await SubtractSpecialtyBookedAppointmentsAsync(cached, request);

            // Update held counts in real-time
            await UpdateSpecialtyHeldCountsAsync(cached, request, currentUserId);

            // Filter out slots with no remaining capacity
            cached.AvailableSlots = cached.AvailableSlots.Where(s => s.IsAvailable).ToList();

            return cached;
        }

        // Step 1: Get all doctor IDs by hospital and specialty via gRPC (optimized - only IDs)
        // Filter by appointment type to only get doctors who support this service type
        var getDoctorIdsRequest = new BookingCare.Services.Doctor.Protos.GetDoctorIdsByHospitalAndSpecialtyRequest
        {
            HospitalId = request.HospitalId.ToString(),
            SpecialtyId = request.SpecialtyId.ToString(),
            AppointmentType = request.AppointmentType == AppointmentType.IN_PERSON ? "Khám trực tiếp" : "Tư vấn trực tuyến" // "IN_PERSON" or "TELEHEALTH"
        };

        var doctorIdsResponse = await _grpcClients.DoctorClient.GetDoctorIdsByHospitalAndSpecialtyAsync(getDoctorIdsRequest);

        if (doctorIdsResponse.DoctorIds.Count == 0)
        {
            _logger.LogInformation("No doctors found for hospital {HospitalId}, specialty {SpecialtyId}",
                request.HospitalId, request.SpecialtyId);

            return new SpecialtyAvailableSlotsResponseDto
            {
                HospitalId = request.HospitalId,
                SpecialtyId = request.SpecialtyId,
                Date = request.Date,
                AppointmentType = request.AppointmentType,
                TotalDoctorsAvailable = 0,
                AvailableSlots = new List<SpecialtyAvailableSlotDto>()
            };
        }

        var doctorIds = doctorIdsResponse.DoctorIds.Select(Guid.Parse).ToList();
        _logger.LogInformation("Found {Count} doctors for hospital {HospitalId}, specialty {SpecialtyId}",
            doctorIds.Count, request.HospitalId, request.SpecialtyId);

        // Step 2: Get available slots for all doctors using optimized batch processing
        // This reduces N+1 queries by fetching data in bulk
        var (slotDoctorCounts, doctorsWithSlots) = await GetBatchDoctorAvailableSlotsAsync(doctorIds, request.Date, currentUserId);

        // Step 3: Build response with capacity information
        var availableSlots = slotDoctorCounts
            .OrderBy(kvp => (int)kvp.Key)
            .Select(kvp =>
            {
                var slotDto = Utilities.AppointmentTimeHelper.ConvertEnumToDto(kvp.Key);
                return new SpecialtyAvailableSlotDto
                {
                    Id = slotDto.Id,
                    StartTime = slotDto.StartTime,
                    EndTime = slotDto.EndTime,
                    AvailableDoctorCount = kvp.Value,
                    HeldCount = 0 // Will be updated below
                };
            })
            .ToList();

        var response = new SpecialtyAvailableSlotsResponseDto
        {
            HospitalId = request.HospitalId,
            SpecialtyId = request.SpecialtyId,
            Date = request.Date,
            AppointmentType = request.AppointmentType,
            TotalDoctorsAvailable = doctorsWithSlots,
            AvailableSlots = availableSlots
        };

        // IMPORTANT: Cache the RAW response BEFORE subtracting booked appointments
        // This ensures that when reading from cache, we can still subtract real-time booked counts
        // Without this, we would double-subtract: once when caching, once when reading from cache
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromMinutes(1));

        _logger.LogDebug("Cached RAW specialty slots (before subtraction) for hospital {HospitalId}, specialty {SpecialtyId}",
            request.HospitalId, request.SpecialtyId);

        // Step 5: Subtract booked appointments from available counts
        // This is CRITICAL: appointments with PENDING/CONFIRMED status reduce capacity
        await SubtractSpecialtyBookedAppointmentsAsync(response, request);

        // Step 6: Update held counts from specialty hold slots
        await UpdateSpecialtyHeldCountsAsync(response, request, currentUserId);

        // Step 7: Filter out slots with no remaining capacity
        response.AvailableSlots = response.AvailableSlots.Where(s => s.IsAvailable).ToList();

        _logger.LogInformation(
            "Specialty available slots for hospital {HospitalId}, specialty {SpecialtyId} on {Date}: {TotalDoctors} doctors, {AvailableSlots} slots",
            request.HospitalId, request.SpecialtyId, request.Date,
            response.TotalDoctorsAvailable, response.AvailableSlots.Count);

        return response;
    }

    /// <summary>
    /// Subtract booked appointments from available doctor counts
    /// This ensures that PENDING/CONFIRMED appointments reduce the available capacity
    /// </summary>
    private async Task SubtractSpecialtyBookedAppointmentsAsync(
        SpecialtyAvailableSlotsResponseDto response,
        GetSpecialtyAvailableSlotsRequest request)
    {
        try
        {
            // Call Appointment service via gRPC to get booked counts per time slot
            var grpcRequest = new BookingCare.Services.Appointment.Protos.CheckSpecialtyBookedSlotsRequest
            {
                HospitalId = request.HospitalId.ToString(),
                SpecialtyId = request.SpecialtyId.ToString(),
                AppointmentDate = request.Date.ToString(DateFormat),
                AppointmentType = (int)request.AppointmentType
            };

            var bookedResponse = await _grpcClients.AppointmentClient.CheckSpecialtyBookedSlotsAsync(grpcRequest);

            // Create a dictionary for quick lookup
            var bookedCounts = bookedResponse.BookedSlots
                .ToDictionary(s => s.AppointmentTimeId, s => s.BookedCount);

            // Subtract booked counts from available doctor counts
            foreach (var slot in response.AvailableSlots)
            {
                var slotBytes = slot.Id.ToByteArray();
                var enumValue = BitConverter.ToInt32(slotBytes, 0);

                if (bookedCounts.TryGetValue(enumValue, out var bookedCount))
                {
                    // Reduce available count by number of booked appointments
                    slot.AvailableDoctorCount = Math.Max(0, slot.AvailableDoctorCount - bookedCount);

                    _logger.LogDebug(
                        "Slot {SlotId} for specialty {SpecialtyId}: reduced by {BookedCount} booked appointments, remaining: {Remaining}",
                        enumValue, request.SpecialtyId, bookedCount, slot.AvailableDoctorCount);
                }
            }

            _logger.LogInformation(
                "Subtracted {TotalBooked} booked appointments across {SlotCount} slots for specialty {SpecialtyId}",
                bookedCounts.Values.Sum(), bookedCounts.Count, request.SpecialtyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get booked appointments for specialty {SpecialtyId}, continuing without subtraction",
                request.SpecialtyId);
            // Don't throw - continue with current counts (may show more availability than actual)
        }
    }

    /// <summary>
    /// Update held counts for specialty slots from Redis
    /// </summary>
    private async Task UpdateSpecialtyHeldCountsAsync(
        SpecialtyAvailableSlotsResponseDto response,
        GetSpecialtyAvailableSlotsRequest request,
        Guid? currentUserId)
    {
        foreach (var slot in response.AvailableSlots)
        {
            // Get held count for this specialty slot
            var slotBytes = slot.Id.ToByteArray();
            var enumValue = BitConverter.ToInt32(slotBytes, 0);
            var appointmentTime = (Shared.Common.Enums.AppointmentTime)enumValue;

            var heldCount = await _holdSlotService.GetSpecialtyHeldCountAsync(
                request.HospitalId,
                request.SpecialtyId,
                request.Date,
                appointmentTime,
                currentUserId ?? Guid.Empty);

            slot.HeldCount = heldCount;
        }
    }

    /// <summary>
    /// Get available slots for multiple doctors in batch - optimized to reduce N+1 queries
    /// Returns a dictionary of AppointmentTime -> count of available doctors
    /// </summary>
    private async Task<(Dictionary<Shared.Common.Enums.AppointmentTime, int> SlotCounts, int DoctorsWithSlots)> GetBatchDoctorAvailableSlotsAsync(
        List<Guid> doctorIds,
        DateOnly date,
        Guid? currentUserId)
    {
        var slotDoctorCounts = new Dictionary<Shared.Common.Enums.AppointmentTime, int>();
        var doctorsWithSlots = 0;

        try
        {
            // Step 1: Get all schedules for all doctors in one batch query
            var allDoctorSlots = await _repository.GetAvailableSlotsForDoctorsAsync(doctorIds, date);

            _logger.LogDebug("Batch query returned {Count} doctor-slot combinations for {DoctorCount} doctors on {Date}",
                allDoctorSlots.Sum(d => d.Value.Count), doctorIds.Count, date);

            // Step 2: Get all booked slots for all doctors in one gRPC call
            var bookedSlotsByDoctor = new Dictionary<Guid, HashSet<int>>();
            try
            {
                // Call appointment service to get booked slots for all doctors
                foreach (var doctorId in doctorIds)
                {
                    var checkBookedRequest = new BookingCare.Services.Appointment.Protos.CheckBookedSlotsRequest
                    {
                        DoctorId = doctorId.ToString(),
                        AppointmentDate = date.ToString(DateFormat)
                    };
                    var bookedResponse = await _grpcClients.AppointmentClient.CheckBookedSlotsAsync(checkBookedRequest);
                    bookedSlotsByDoctor[doctorId] = new HashSet<int>(bookedResponse.BookedAppointmentTimeIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get booked slots, continuing without filtering");
            }

            // Step 3: Get all held slots for all doctors in batch
            var heldSlotsByDoctor = new Dictionary<Guid, HashSet<int>>();
            foreach (var doctorId in doctorIds)
            {
                try
                {
                    var heldSlots = await _holdSlotService.GetHeldSlotsAsync(doctorId, date, currentUserId ?? Guid.Empty);
                    heldSlotsByDoctor[doctorId] = new HashSet<int>(heldSlots.Select(hs => (int)hs));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get held slots for doctor {DoctorId}", doctorId);
                    heldSlotsByDoctor[doctorId] = new HashSet<int>();
                }
            }

            // Step 4: Aggregate available slots per time slot
            foreach (var doctorId in doctorIds)
            {
                if (!allDoctorSlots.TryGetValue(doctorId, out var doctorSlots) || !doctorSlots.Any())
                {
                    continue;
                }

                var bookedSlots = bookedSlotsByDoctor.GetValueOrDefault(doctorId, new HashSet<int>());
                var heldSlots = heldSlotsByDoctor.GetValueOrDefault(doctorId, new HashSet<int>());
                var hasAvailableSlot = false;

                foreach (var appointmentTime in doctorSlots)
                {
                    var timeId = (int)appointmentTime;

                    // Skip if slot is booked or held
                    if (bookedSlots.Contains(timeId) || heldSlots.Contains(timeId))
                    {
                        continue;
                    }

                    // Count this doctor as available for this time slot
                    if (!slotDoctorCounts.ContainsKey(appointmentTime))
                    {
                        slotDoctorCounts[appointmentTime] = 0;
                    }
                    slotDoctorCounts[appointmentTime]++;
                    hasAvailableSlot = true;
                }

                if (hasAvailableSlot)
                {
                    doctorsWithSlots++;
                }
            }

            _logger.LogInformation(
                "Batch processing complete: {DoctorsWithSlots} doctors with available slots, {SlotCount} unique time slots",
                doctorsWithSlots, slotDoctorCounts.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in batch doctor available slots processing");
        }

        return (slotDoctorCounts, doctorsWithSlots);
    }

    #endregion
}

