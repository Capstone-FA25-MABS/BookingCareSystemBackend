using Grpc.Core;
using BookingCare.Services.Schedule.Protos;
using Google.Protobuf.WellKnownTypes;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// gRPC service implementation for Schedule Service
/// </summary>
public class ScheduleGrpcService : Protos.ScheduleService.ScheduleServiceBase
{
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<ScheduleGrpcService> _logger;

    public ScheduleGrpcService(IScheduleService scheduleService, ILogger<ScheduleGrpcService> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    public override async Task<GetAvailableSlotsResponse> GetAvailableSlots(GetAvailableSlotsRequest request, ServerCallContext context)
    {
        try
        {
            var serviceRequest = new Models.Requests.GetAvailableSlotsRequest
            {
                DoctorId = request.DoctorId,
                Date = DateOnly.Parse(request.Date),
                ServiceId = request.ServiceId == 0 ? null : request.ServiceId
            };

            var slots = await _scheduleService.GetAvailableSlotsAsync(serviceRequest);
            
            var response = new GetAvailableSlotsResponse
            {
                Success = true,
                Message = "Available slots retrieved successfully"
            };

            foreach (var slot in slots)
            {
                response.Slots.Add(new AppointmentTime
                {
                    Id = slot.Id,
                    StartTime = slot.StartTime,
                    EndTime = slot.EndTime
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available slots for doctor {DoctorId} on {Date}", request.DoctorId, request.Date);
            return new GetAvailableSlotsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<GetDoctorDailyScheduleResponse> GetDoctorDailySchedule(GetDoctorDailyScheduleRequest request, ServerCallContext context)
    {
        try
        {
            var schedule = await _scheduleService.GetDoctorDailyScheduleAsync(request.DoctorId, DateOnly.Parse(request.Date));
            
            if (schedule == null)
            {
                return new GetDoctorDailyScheduleResponse
                {
                    Success = false,
                    Message = "No schedule found for the specified doctor and date"
                };
            }

            var response = new GetDoctorDailyScheduleResponse
            {
                Success = true,
                Message = "Schedule retrieved successfully",
                Schedule = MapToDoctorDailySchedule(schedule)
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor schedule for doctor {DoctorId} on {Date}", request.DoctorId, request.Date);
            return new GetDoctorDailyScheduleResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<GetDoctorScheduleRangeResponse> GetDoctorScheduleRange(GetDoctorScheduleRangeRequest request, ServerCallContext context)
    {
        try
        {
            var serviceRequest = new Models.Requests.GetDoctorScheduleRequest
            {
                DoctorId = request.DoctorId,
                StartDate = DateOnly.Parse(request.StartDate),
                EndDate = DateOnly.Parse(request.EndDate)
            };

            var schedules = await _scheduleService.GetDoctorScheduleRangeAsync(serviceRequest);
            
            var response = new GetDoctorScheduleRangeResponse
            {
                Success = true,
                Message = "Schedule range retrieved successfully"
            };

            foreach (var schedule in schedules)
            {
                response.Schedules.Add(MapToDoctorDailySchedule(schedule));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor schedule range for doctor {DoctorId}", request.DoctorId);
            return new GetDoctorScheduleRangeResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<DoctorDailyScheduleResponse> CreateOrUpdateDoctorDailySchedule(CreateDoctorDailyScheduleRequest request, ServerCallContext context)
    {
        try
        {
            var serviceRequest = new Models.Requests.CreateDoctorDailyScheduleRequest
            {
                DoctorId = request.DoctorId,
                ScheduleDate = DateOnly.Parse(request.ScheduleDate),
                PatternId = request.PatternId
            };

            var schedule = await _scheduleService.CreateOrUpdateDoctorDailyScheduleAsync(serviceRequest);
            
            var response = new DoctorDailyScheduleResponse
            {
                Success = true,
                Message = "Doctor schedule created/updated successfully",
                Schedule = MapToDoctorDailySchedule(schedule)
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating doctor schedule for doctor {DoctorId}", request.DoctorId);
            return new DoctorDailyScheduleResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<DoctorScheduleExceptionResponse> CreateDoctorScheduleException(CreateDoctorScheduleExceptionRequest request, ServerCallContext context)
    {
        try
        {
            var serviceRequest = new Models.Requests.CreateDoctorScheduleExceptionRequest
            {
                DoctorId = request.DoctorId,
                ExceptionDate = DateOnly.Parse(request.ExceptionDate),
                AppointmentTimeId = request.AppointmentTimeId == 0 ? null : request.AppointmentTimeId,
                ExceptionType = System.Enum.Parse<Models.Entities.ExceptionType>(request.ExceptionType),
                IsAvailable = request.IsAvailable,
                Reason = request.Reason
            };

            var exception = await _scheduleService.CreateDoctorScheduleExceptionAsync(serviceRequest);
            
            var response = new DoctorScheduleExceptionResponse
            {
                Success = true,
                Message = "Doctor schedule exception created successfully",
                Exception = MapToDoctorScheduleException(exception)
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor schedule exception for doctor {DoctorId}", request.DoctorId);
            return new DoctorScheduleExceptionResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<GetAllAppointmentTimesResponse> GetAllAppointmentTimes(GetAllAppointmentTimesRequest request, ServerCallContext context)
    {
        try
        {
            var appointmentTimes = await _scheduleService.GetAllAppointmentTimesAsync();
            
            var response = new GetAllAppointmentTimesResponse
            {
                Success = true,
                Message = "Appointment times retrieved successfully"
            };

            foreach (var appointmentTime in appointmentTimes)
            {
                response.AppointmentTimes.Add(new AppointmentTime
                {
                    Id = appointmentTime.Id,
                    StartTime = appointmentTime.StartTime,
                    EndTime = appointmentTime.EndTime
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all appointment times");
            return new GetAllAppointmentTimesResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    public override async Task<GetAllSchedulePatternsResponse> GetAllSchedulePatterns(GetAllSchedulePatternsRequest request, ServerCallContext context)
    {
        try
        {
            var patterns = await _scheduleService.GetAllSchedulePatternsAsync();
            
            var response = new GetAllSchedulePatternsResponse
            {
                Success = true,
                Message = "Schedule patterns retrieved successfully"
            };

            foreach (var pattern in patterns)
            {
                response.Patterns.Add(MapToSchedulePattern(pattern));
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all schedule patterns");
            return new GetAllSchedulePatternsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    #region Mapping Methods

    private static DoctorDailySchedule MapToDoctorDailySchedule(Models.DTOs.DoctorDailyScheduleDto dto)
    {
        var schedule = new DoctorDailySchedule
        {
            Id = dto.Id,
            DoctorId = dto.DoctorId,
            ScheduleDate = dto.ScheduleDate.ToString("yyyy-MM-dd"),
            PatternId = dto.PatternId,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.UpdatedAt.ToUniversalTime())
        };

        if (dto.Pattern != null)
        {
            schedule.Pattern = MapToSchedulePattern(dto.Pattern);
        }

        return schedule;
    }

    private static SchedulePattern MapToSchedulePattern(Models.DTOs.SchedulePatternDto dto)
    {
        var pattern = new SchedulePattern
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.UpdatedAt.ToUniversalTime())
        };

        foreach (var slot in dto.Slots)
        {
            pattern.Slots.Add(new AppointmentTime
            {
                Id = slot.Id,
                StartTime = slot.StartTime,
                EndTime = slot.EndTime
            });
        }

        return pattern;
    }

    private static DoctorScheduleException MapToDoctorScheduleException(Models.DTOs.DoctorScheduleExceptionDto dto)
    {
        var exception = new DoctorScheduleException
        {
            Id = dto.Id,
            DoctorId = dto.DoctorId,
            ExceptionDate = dto.ExceptionDate.ToString("yyyy-MM-dd"),
            AppointmentTimeId = dto.AppointmentTimeId ?? 0,
            ExceptionType = dto.ExceptionType,
            IsAvailable = dto.IsAvailable,
            Reason = dto.Reason ?? string.Empty,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime())
        };

        if (dto.AppointmentTime != null)
        {
            exception.AppointmentTime = new AppointmentTime
            {
                Id = dto.AppointmentTime.Id,
                StartTime = dto.AppointmentTime.StartTime,
                EndTime = dto.AppointmentTime.EndTime
            };
        }

        return exception;
    }

    #endregion
}
