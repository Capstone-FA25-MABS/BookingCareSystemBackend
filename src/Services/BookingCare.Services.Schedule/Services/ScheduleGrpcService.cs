using Grpc.Core;
using BookingCare.Services.Schedule.Protos;
using BookingCare.Services.Schedule.Enums;
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
                // Convert single pattern ID to list for backward compatibility
                SchedulePatterns = new List<SchedulePatterns> { (SchedulePatterns)request.PatternId }
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
                AppointmentTime = request.AppointmentTimeId == 0 ? null : (BookingCare.Shared.Common.Enums.AppointmentTime)request.AppointmentTimeId,
                ExceptionType = System.Enum.Parse<ExceptionType>(request.ExceptionType),
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

    public override Task<GetAllAppointmentTimesResponse> GetAllAppointmentTimes(GetAllAppointmentTimesRequest request, ServerCallContext context)
    {
        try
        {
            // Return all predefined appointment time enum values
            var appointmentTimeEnums = System.Enum.GetValues<BookingCare.Shared.Common.Enums.AppointmentTime>();

            var response = new GetAllAppointmentTimesResponse
            {
                Success = true,
                Message = "Appointment times retrieved successfully"
            };

            foreach (var appointmentTimeEnum in appointmentTimeEnums)
            {
                var (startTime, endTime) = GetTimeStringsFromEnum(appointmentTimeEnum);
                response.AppointmentTimes.Add(new AppointmentTime
                {
                    Id = (int)appointmentTimeEnum,
                    StartTime = startTime,
                    EndTime = endTime
                });
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all appointment times");
            return Task.FromResult(new GetAllAppointmentTimesResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    public override Task<GetAllSchedulePatternsResponse> GetAllSchedulePatterns(GetAllSchedulePatternsRequest request, ServerCallContext context)
    {
        try
        {
            var response = new GetAllSchedulePatternsResponse
            {
                Success = true,
                Message = "Schedule patterns retrieved successfully"
            };

            // Return predefined schedule patterns from enum
            var patterns = new[]
            {
                new SchedulePattern { Id = (int)SchedulePatterns.MORNING, Name = "Morning", Description = "Morning pattern (08:00 - 12:00)", CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow), UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow) },
                new SchedulePattern { Id = (int)SchedulePatterns.AFTERNOON, Name = "Afternoon", Description = "Afternoon pattern (13:00 - 17:00)", CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow), UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow) },
                new SchedulePattern { Id = (int)SchedulePatterns.EVENING, Name = "Evening", Description = "Evening pattern (17:00 - 21:00)", CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow), UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow) },
                new SchedulePattern { Id = (int)SchedulePatterns.FULL_DAY, Name = "Full Day", Description = "Full day pattern (08:00 - 21:00)", CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow), UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow) }
            };

            foreach (var pattern in patterns)
            {
                response.Patterns.Add(pattern);
            }

            return Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all schedule patterns");
            return Task.FromResult(new GetAllSchedulePatternsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            });
        }
    }

    #region Mapping Methods

    private static DoctorDailySchedule MapToDoctorDailySchedule(Models.DTOs.DoctorDailyScheduleDto dto)
    {
        // For backward compatibility, use the first pattern when multiple patterns exist
        var primaryPattern = dto.SchedulePatterns.FirstOrDefault();
        
        var schedule = new DoctorDailySchedule
        {
            Id = dto.Id,
            DoctorId = dto.DoctorId,
            ScheduleDate = dto.ScheduleDate.ToString("yyyy-MM-dd"),
            PatternId = (long)primaryPattern,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime()),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.UpdatedAt.ToUniversalTime())
        };

        // Set pattern details based on primary pattern
        schedule.Pattern = new SchedulePattern
        {
            Id = (long)primaryPattern,
            Name = primaryPattern.ToString(),
            Description = GetSchedulePatternDescription(primaryPattern),
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow),
            UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
        };

        return schedule;
    }

    private static string GetSchedulePatternDescription(SchedulePatterns pattern)
    {
        return pattern switch
        {
            SchedulePatterns.MORNING => "Morning pattern (08:00 - 12:00)",
            SchedulePatterns.AFTERNOON => "Afternoon pattern (13:00 - 17:00)",
            SchedulePatterns.EVENING => "Evening pattern (17:00 - 21:00)",
            SchedulePatterns.FULL_DAY => "Full day pattern (08:00 - 21:00)",
            _ => "Unknown pattern"
        };
    }

    private static DoctorScheduleException MapToDoctorScheduleException(Models.DTOs.DoctorScheduleExceptionDto dto)
    {
        var exception = new DoctorScheduleException
        {
            Id = dto.Id,
            DoctorId = dto.DoctorId,
            ExceptionDate = dto.ExceptionDate.ToString("yyyy-MM-dd"),
            AppointmentTimeId = dto.AppointmentTime.HasValue ? (int)dto.AppointmentTime.Value : 0,
            ExceptionType = dto.ExceptionType,
            IsAvailable = dto.IsAvailable,
            Reason = dto.Reason ?? string.Empty,
            CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(dto.CreatedAt.ToUniversalTime())
        };

        if (dto.AppointmentTime != null)
        {
            var (startTime, endTime) = GetTimeStringsFromEnum(dto.AppointmentTime.Value);
            exception.AppointmentTime = new AppointmentTime
            {
                Id = (int)dto.AppointmentTime.Value,
                StartTime = startTime,
                EndTime = endTime
            };
        }

        return exception;
    }

    private static (string startTime, string endTime) GetTimeStringsFromEnum(Shared.Common.Enums.AppointmentTime appointmentTime)
    {
        return appointmentTime switch
        {
            // Range time 30 minutes
            Shared.Common.Enums.AppointmentTime.AT_08_00_08_30 => ("08:00", "08:30"),
            Shared.Common.Enums.AppointmentTime.AT_08_30_09_00 => ("08:30", "09:00"),
            Shared.Common.Enums.AppointmentTime.AT_09_00_09_30 => ("09:00", "09:30"),
            Shared.Common.Enums.AppointmentTime.AT_09_30_10_00 => ("09:30", "10:00"),
            Shared.Common.Enums.AppointmentTime.AT_10_00_10_30 => ("10:00", "10:30"),
            Shared.Common.Enums.AppointmentTime.AT_10_30_11_00 => ("10:30", "11:00"),
            Shared.Common.Enums.AppointmentTime.AT_11_00_11_30 => ("11:00", "11:30"),
            Shared.Common.Enums.AppointmentTime.AT_11_30_12_00 => ("11:30", "12:00"),
            Shared.Common.Enums.AppointmentTime.AT_13_00_13_30 => ("13:00", "13:30"),
            Shared.Common.Enums.AppointmentTime.AT_13_30_14_00 => ("13:30", "14:00"),
            Shared.Common.Enums.AppointmentTime.AT_14_00_14_30 => ("14:00", "14:30"),
            Shared.Common.Enums.AppointmentTime.AT_14_30_15_00 => ("14:30", "15:00"),
            Shared.Common.Enums.AppointmentTime.AT_15_00_15_30 => ("15:00", "15:30"),
            Shared.Common.Enums.AppointmentTime.AT_15_30_16_00 => ("15:30", "16:00"),
            Shared.Common.Enums.AppointmentTime.AT_16_00_16_30 => ("16:00", "16:30"),
            Shared.Common.Enums.AppointmentTime.AT_16_30_17_00 => ("16:30", "17:00"),
            Shared.Common.Enums.AppointmentTime.AT_17_00_17_30 => ("17:00", "17:30"),
            Shared.Common.Enums.AppointmentTime.AT_17_30_18_00 => ("17:30", "18:00"),
            Shared.Common.Enums.AppointmentTime.AT_18_00_18_30 => ("18:00", "18:30"),
            Shared.Common.Enums.AppointmentTime.AT_18_30_19_00 => ("18:30", "19:00"),
            Shared.Common.Enums.AppointmentTime.AT_19_00_19_30 => ("19:00", "19:30"),
            Shared.Common.Enums.AppointmentTime.AT_19_30_20_00 => ("19:30", "20:00"),
            Shared.Common.Enums.AppointmentTime.AT_20_00_20_30 => ("20:00", "20:30"),
            Shared.Common.Enums.AppointmentTime.AT_20_30_21_00 => ("20:30", "21:00"),
            Shared.Common.Enums.AppointmentTime.AT_21_00_21_30 => ("21:00", "21:30"),
            Shared.Common.Enums.AppointmentTime.AT_21_30_22_00 => ("21:30", "22:00"),
            Shared.Common.Enums.AppointmentTime.AT_22_00_22_30 => ("22:00", "22:30"),
            Shared.Common.Enums.AppointmentTime.AT_22_30_23_00 => ("22:30", "23:00"),

            // Range time one hour
            Shared.Common.Enums.AppointmentTime.AT_08_00_09_00 => ("08:00", "09:00"),
            Shared.Common.Enums.AppointmentTime.AT_09_00_10_00 => ("09:00", "10:00"),
            Shared.Common.Enums.AppointmentTime.AT_10_00_11_00 => ("10:00", "11:00"),
            Shared.Common.Enums.AppointmentTime.AT_11_00_12_00 => ("11:00", "12:00"),
            Shared.Common.Enums.AppointmentTime.AT_13_00_14_00 => ("13:00", "14:00"),
            Shared.Common.Enums.AppointmentTime.AT_14_00_15_00 => ("14:00", "15:00"),
            Shared.Common.Enums.AppointmentTime.AT_15_00_16_00 => ("15:00", "16:00"),
            Shared.Common.Enums.AppointmentTime.AT_16_00_17_00 => ("16:00", "17:00"),
            Shared.Common.Enums.AppointmentTime.AT_17_00_18_00 => ("17:00", "18:00"),
            Shared.Common.Enums.AppointmentTime.AT_18_00_19_00 => ("18:00", "19:00"),
            Shared.Common.Enums.AppointmentTime.AT_19_00_20_00 => ("19:00", "20:00"),
            Shared.Common.Enums.AppointmentTime.AT_20_00_21_00 => ("20:00", "21:00"),
            Shared.Common.Enums.AppointmentTime.AT_21_00_22_00 => ("21:00", "22:00"),
            Shared.Common.Enums.AppointmentTime.AT_22_00_23_00 => ("22:00", "23:00"),
            _ => ("Unknown", "Unknown")
        };
    }

    #endregion
}
