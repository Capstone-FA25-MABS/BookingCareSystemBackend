using BookingCare.Services.Schedule.Protos;
using Grpc.Core;
using Google.Protobuf.WellKnownTypes;
using ModelRequests = BookingCare.Services.Schedule.Models.Requests;

namespace BookingCare.Services.Schedule.Services;

/// <summary>
/// gRPC service implementation for Schedule operations with GUID support
/// </summary>
public class ScheduleGrpcService : Protos.ScheduleService.ScheduleServiceBase
{
    private const string InvalidDoctorIdFormatMessage = "Invalid doctor ID format";
    private readonly IScheduleService _scheduleService;
    private readonly IHoldSlotService _holdSlotService;
    private readonly ILogger<ScheduleGrpcService> _logger;

    public ScheduleGrpcService(
        IScheduleService scheduleService,
        IHoldSlotService holdSlotService,
        ILogger<ScheduleGrpcService> logger)
    {
        _scheduleService = scheduleService;
        _holdSlotService = holdSlotService;
        _logger = logger;
    }

    /// <summary>
    /// Get available appointment slots for a doctor on a specific date
    /// </summary>
    public override async Task<GetAvailableSlotsResponse> GetAvailableSlots(GetAvailableSlotsRequest request, ServerCallContext context)
    {
        try
        {
            // Validate and parse GUID inputs
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                return new GetAvailableSlotsResponse
                {
                    Success = false,
                    Message = InvalidDoctorIdFormatMessage
                };
            }

            if (!DateOnly.TryParse(request.Date, out var date))
            {
                return new GetAvailableSlotsResponse
                {
                    Success = false,
                    Message = "Invalid date format. Expected YYYY-MM-DD"
                };
            }

            Guid? serviceId = null;
            if (!string.IsNullOrEmpty(request.ServiceId))
            {
                if (!Guid.TryParse(request.ServiceId, out var parsedServiceId))
                {
                    return new GetAvailableSlotsResponse
                    {
                        Success = false,
                        Message = "Invalid service ID format"
                    };
                }
                serviceId = parsedServiceId;
            }

            // Create request model using the Models namespace
            var availableSlotsRequest = new ModelRequests.GetAvailableSlotsRequest
            {
                DoctorId = doctorId,
                Date = date,
                ServiceId = serviceId
            };

            // Get available slots
            var slots = await _scheduleService.GetAvailableSlotsAsync(availableSlotsRequest);

            // Convert to proto response
            var response = new GetAvailableSlotsResponse
            {
                Success = true,
                Message = "Available slots retrieved successfully"
            };

            foreach (var slot in slots)
            {
                response.Slots.Add(new AppointmentTime
                {
                    Id = slot.Id.ToString(),
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
                Message = "An error occurred while retrieving available slots"
            };
        }
    }

    /// <summary>
    /// Get doctor's daily schedule
    /// </summary>
    public override async Task<GetDoctorDailyScheduleResponse> GetDoctorDailySchedule(GetDoctorDailyScheduleRequest request, ServerCallContext context)
    {
        try
        {
            // Validate and parse inputs
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                return new GetDoctorDailyScheduleResponse
                {
                    Success = false,
                    Message = InvalidDoctorIdFormatMessage
                };
            }

            if (!DateOnly.TryParse(request.Date, out var date))
            {
                return new GetDoctorDailyScheduleResponse
                {
                    Success = false,
                    Message = "Invalid date format. Expected YYYY-MM-DD"
                };
            }

            // Get schedule
            var schedule = await _scheduleService.GetDoctorDailyScheduleAsync(doctorId, date);

            if (schedule == null)
            {
                return new GetDoctorDailyScheduleResponse
                {
                    Success = false,
                    Message = "Schedule not found"
                };
            }

            // Convert to proto response
            var response = new GetDoctorDailyScheduleResponse
            {
                Success = true,
                Message = "Schedule retrieved successfully",
                Schedule = new DoctorDailySchedule
                {
                    Id = schedule.Id.ToString(),
                    DoctorId = schedule.DoctorId.ToString(),
                    ScheduleDate = schedule.ScheduleDate.ToString("yyyy-MM-dd"),
                    CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.CreatedAt, DateTimeKind.Utc)),
                    UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.UpdatedAt, DateTimeKind.Utc))
                }
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor daily schedule for doctor {DoctorId} on {Date}", request.DoctorId, request.Date);
            return new GetDoctorDailyScheduleResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the schedule"
            };
        }
    }

    /// <summary>
    /// Create or update doctor's daily schedule
    /// </summary>
    public override async Task<DoctorDailyScheduleResponse> CreateOrUpdateDoctorDailySchedule(CreateDoctorDailyScheduleRequest request, ServerCallContext context)
    {
        try
        {
            // Validate and parse inputs
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                return new DoctorDailyScheduleResponse
                {
                    Success = false,
                    Message = InvalidDoctorIdFormatMessage
                };
            }

            if (!DateOnly.TryParse(request.ScheduleDate, out var scheduleDate))
            {
                return new DoctorDailyScheduleResponse
                {
                    Success = false,
                    Message = "Invalid schedule date format. Expected YYYY-MM-DD"
                };
            }

            // Create request model using the Models namespace
            var createRequest = new ModelRequests.CreateDoctorDailyScheduleRequest
            {
                DoctorId = doctorId,
                ScheduleDate = scheduleDate,
                SchedulePatterns = new List<BookingCare.Services.Schedule.Enums.SchedulePatterns>()
            };

            // Create or update schedule
            var schedule = await _scheduleService.CreateOrUpdateDoctorDailyScheduleAsync(createRequest);

            // Convert to proto response
            var response = new DoctorDailyScheduleResponse
            {
                Success = true,
                Message = "Schedule created/updated successfully",
                Schedule = new DoctorDailySchedule
                {
                    Id = schedule.Id.ToString(),
                    DoctorId = schedule.DoctorId.ToString(),
                    ScheduleDate = schedule.ScheduleDate.ToString("yyyy-MM-dd"),
                    CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.CreatedAt, DateTimeKind.Utc)),
                    UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.UpdatedAt, DateTimeKind.Utc))
                }
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating doctor daily schedule for doctor {DoctorId} on {ScheduleDate}",
                request.DoctorId, request.ScheduleDate);
            return new DoctorDailyScheduleResponse
            {
                Success = false,
                Message = "An error occurred while creating/updating the schedule"
            };
        }
    }

    /// <summary>
    /// Get doctor's schedule for a date range (simplified implementation)
    /// </summary>
    public override async Task<GetDoctorScheduleRangeResponse> GetDoctorScheduleRange(GetDoctorScheduleRangeRequest request, ServerCallContext context)
    {
        try
        {
            // Validate and parse inputs
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                return new GetDoctorScheduleRangeResponse
                {
                    Success = false,
                    Message = InvalidDoctorIdFormatMessage
                };
            }

            if (!DateOnly.TryParse(request.StartDate, out var startDate))
            {
                return new GetDoctorScheduleRangeResponse
                {
                    Success = false,
                    Message = "Invalid start date format. Expected YYYY-MM-DD"
                };
            }

            if (!DateOnly.TryParse(request.EndDate, out var endDate))
            {
                return new GetDoctorScheduleRangeResponse
                {
                    Success = false,
                    Message = "Invalid end date format. Expected YYYY-MM-DD"
                };
            }

            // Create request model
            var scheduleRequest = new ModelRequests.GetDoctorScheduleRequest
            {
                DoctorId = doctorId,
                StartDate = startDate,
                EndDate = endDate
            };

            // Get schedules
            var schedules = await _scheduleService.GetDoctorScheduleRangeAsync(scheduleRequest);

            // Convert to proto response
            var response = new GetDoctorScheduleRangeResponse
            {
                Success = true,
                Message = "Schedules retrieved successfully"
            };

            foreach (var schedule in schedules)
            {
                response.Schedules.Add(new DoctorDailySchedule
                {
                    Id = schedule.Id.ToString(),
                    DoctorId = schedule.DoctorId.ToString(),
                    ScheduleDate = schedule.ScheduleDate.ToString("yyyy-MM-dd"),
                    CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.CreatedAt, DateTimeKind.Utc)),
                    UpdatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(schedule.UpdatedAt, DateTimeKind.Utc))
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor schedule range for doctor {DoctorId} from {StartDate} to {EndDate}",
                request.DoctorId, request.StartDate, request.EndDate);
            return new GetDoctorScheduleRangeResponse
            {
                Success = false,
                Message = "An error occurred while retrieving the schedule range"
            };
        }
    }

    /// <summary>
    /// Create doctor schedule exception (simplified implementation)
    /// </summary>
    public override Task<DoctorScheduleExceptionResponse> CreateDoctorScheduleException(CreateDoctorScheduleExceptionRequest request, ServerCallContext context)
    {
        try
        {
            return Task.FromResult(new DoctorScheduleExceptionResponse
            {
                Success = false,
                Message = "CreateDoctorScheduleException is not implemented yet"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating doctor schedule exception for doctor {DoctorId} on {ExceptionDate}",
                request.DoctorId, request.ExceptionDate);
            return Task.FromResult(new DoctorScheduleExceptionResponse
            {
                Success = false,
                Message = "An error occurred while creating the schedule exception"
            });
        }
    }

    /// <summary>
    /// Get all appointment times
    /// </summary>
    public override async Task<GetAllAppointmentTimesResponse> GetAllAppointmentTimes(GetAllAppointmentTimesRequest request, ServerCallContext context)
    {
        try
        {
            // Get all appointment time enums
            var appointmentTimeEnums = System.Enum.GetValues<BookingCare.Shared.Common.Enums.AppointmentTime>();

            var appointmentTimes = new List<Protos.AppointmentTime>();

            foreach (var enumValue in appointmentTimeEnums)
            {
                var (startTime, endTime) = GetTimeStringsFromEnum(enumValue);

                appointmentTimes.Add(new Protos.AppointmentTime
                {
                    Id = GenerateDeterministicGuid((int)enumValue).ToString(),
                    StartTime = startTime,
                    EndTime = endTime
                });
            }

            var response = new GetAllAppointmentTimesResponse
            {
                Success = true,
                Message = "Appointment times retrieved successfully"
            };
            response.AppointmentTimes.AddRange(appointmentTimes);

            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all appointment times");
            return new GetAllAppointmentTimesResponse
            {
                Success = false,
                Message = "An error occurred while retrieving appointment times"
            };
        }
    }

    /// <summary>
    /// Get all schedule patterns
    /// </summary>
    public override async Task<GetAllSchedulePatternsResponse> GetAllSchedulePatterns(GetAllSchedulePatternsRequest request, ServerCallContext context)
    {
        try
        {
            // Get all schedule pattern enums
            var patternEnums = System.Enum.GetValues<BookingCare.Services.Schedule.Enums.SchedulePatterns>();

            var schedulePatterns = new List<Protos.SchedulePattern>();

            foreach (var enumValue in patternEnums)
            {
                var patternId = GenerateDeterministicGuid((int)enumValue);
                var appointmentSlots = GetAppointmentTimesForPattern(enumValue);

                var pattern = new Protos.SchedulePattern
                {
                    Id = patternId.ToString(),
                    Name = enumValue.ToString(),
                    Description = GetPatternDescription(enumValue),
                    CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow),
                    UpdatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)
                };

                // Add appointment time slots for this pattern
                foreach (var slot in appointmentSlots)
                {
                    var (startTime, endTime) = GetTimeStringsFromEnum(slot);

                    pattern.Slots.Add(new Protos.AppointmentTime
                    {
                        Id = GenerateDeterministicGuid((int)slot).ToString(),
                        StartTime = startTime,
                        EndTime = endTime
                    });
                }

                schedulePatterns.Add(pattern);
            }

            var response = new GetAllSchedulePatternsResponse
            {
                Success = true,
                Message = "Schedule patterns retrieved successfully"
            };
            response.Patterns.AddRange(schedulePatterns);

            return await Task.FromResult(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all schedule patterns");
            return new GetAllSchedulePatternsResponse
            {
                Success = false,
                Message = "An error occurred while retrieving schedule patterns"
            };
        }
    }

    #region Helper Methods

    /// <summary>
    /// Generate a deterministic GUID from an integer value for appointment time slots
    /// </summary>
    private static Guid GenerateDeterministicGuid(int value)
    {
        var bytes = new byte[16];
        var valueBytes = BitConverter.GetBytes(value);
        Array.Copy(valueBytes, 0, bytes, 0, Math.Min(valueBytes.Length, 16));
        return new Guid(bytes);
    }

    /// <summary>
    /// Get time strings from appointment time enum
    /// </summary>
    private static (string startTime, string endTime) GetTimeStringsFromEnum(BookingCare.Shared.Common.Enums.AppointmentTime appointmentTime)
    {
        return appointmentTime switch
        {
            // Range time 30 minutes
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_08_00_08_30 => ("08:00", "08:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_08_30_09_00 => ("08:30", "09:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_09_00_09_30 => ("09:00", "09:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_09_30_10_00 => ("09:30", "10:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_10_00_10_30 => ("10:00", "10:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_10_30_11_00 => ("10:30", "11:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_11_00_11_30 => ("11:00", "11:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_11_30_12_00 => ("11:30", "12:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_13_00_13_30 => ("13:00", "13:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_13_30_14_00 => ("13:30", "14:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_14_00_14_30 => ("14:00", "14:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_14_30_15_00 => ("14:30", "15:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_15_00_15_30 => ("15:00", "15:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_15_30_16_00 => ("15:30", "16:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_16_00_16_30 => ("16:00", "16:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_16_30_17_00 => ("16:30", "17:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_17_00_17_30 => ("17:00", "17:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_17_30_18_00 => ("17:30", "18:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_18_00_18_30 => ("18:00", "18:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_18_30_19_00 => ("18:30", "19:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_19_00_19_30 => ("19:00", "19:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_19_30_20_00 => ("19:30", "20:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_20_00_20_30 => ("20:00", "20:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_20_30_21_00 => ("20:30", "21:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_21_00_21_30 => ("21:00", "21:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_21_30_22_00 => ("21:30", "22:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_22_00_22_30 => ("22:00", "22:30"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_22_30_23_00 => ("22:30", "23:00"),

            // Range time one hour
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_08_00_09_00 => ("08:00", "09:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_09_00_10_00 => ("09:00", "10:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_10_00_11_00 => ("10:00", "11:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_11_00_12_00 => ("11:00", "12:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_13_00_14_00 => ("13:00", "14:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_14_00_15_00 => ("14:00", "15:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_15_00_16_00 => ("15:00", "16:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_16_00_17_00 => ("16:00", "17:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_17_00_18_00 => ("17:00", "18:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_18_00_19_00 => ("18:00", "19:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_19_00_20_00 => ("19:00", "20:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_20_00_21_00 => ("20:00", "21:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_21_00_22_00 => ("21:00", "22:00"),
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_22_00_23_00 => ("22:00", "23:00"),
            _ => ("Unknown", "Unknown")
        };
    }

    /// <summary>
    /// Get appointment times for a specific pattern
    /// </summary>
    private static IEnumerable<BookingCare.Shared.Common.Enums.AppointmentTime> GetAppointmentTimesForPattern(BookingCare.Services.Schedule.Enums.SchedulePatterns pattern)
    {
        return pattern switch
        {
            BookingCare.Services.Schedule.Enums.SchedulePatterns.MORNING => GetMorningSlots(),
            BookingCare.Services.Schedule.Enums.SchedulePatterns.AFTERNOON => GetAfternoonSlots(),
            BookingCare.Services.Schedule.Enums.SchedulePatterns.EVENING => GetEveningSlots(),
            BookingCare.Services.Schedule.Enums.SchedulePatterns.FULL_DAY => GetMorningSlots().Concat(GetAfternoonSlots()).Concat(GetEveningSlots()),
            _ => Enumerable.Empty<BookingCare.Shared.Common.Enums.AppointmentTime>()
        };
    }

    /// <summary>
    /// Get morning appointment slots
    /// </summary>
    private static IEnumerable<BookingCare.Shared.Common.Enums.AppointmentTime> GetMorningSlots()
    {
        return new[]
        {
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_08_00_08_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_08_30_09_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_09_00_09_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_09_30_10_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_10_00_10_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_10_30_11_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_11_00_11_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_11_30_12_00
        };
    }

    /// <summary>
    /// Get afternoon appointment slots
    /// </summary>
    private static IEnumerable<BookingCare.Shared.Common.Enums.AppointmentTime> GetAfternoonSlots()
    {
        return new[]
        {
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_13_00_13_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_13_30_14_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_14_00_14_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_14_30_15_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_15_00_15_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_15_30_16_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_16_00_16_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_16_30_17_00
        };
    }

    /// <summary>
    /// Get evening appointment slots
    /// </summary>
    private static IEnumerable<BookingCare.Shared.Common.Enums.AppointmentTime> GetEveningSlots()
    {
        return new[]
        {
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_17_00_17_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_17_30_18_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_18_00_18_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_18_30_19_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_19_00_19_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_19_30_20_00,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_20_00_20_30,
            BookingCare.Shared.Common.Enums.AppointmentTime.AT_20_30_21_00
        };
    }

    /// <summary>
    /// Get description for schedule pattern
    /// </summary>
    private static string GetPatternDescription(BookingCare.Services.Schedule.Enums.SchedulePatterns pattern)
    {
        return pattern switch
        {
            BookingCare.Services.Schedule.Enums.SchedulePatterns.MORNING => "Morning schedule (08:00 - 12:00)",
            BookingCare.Services.Schedule.Enums.SchedulePatterns.AFTERNOON => "Afternoon schedule (13:00 - 17:00)",
            BookingCare.Services.Schedule.Enums.SchedulePatterns.EVENING => "Evening schedule (17:00 - 21:00)",
            BookingCare.Services.Schedule.Enums.SchedulePatterns.FULL_DAY => "Full day schedule (08:00 - 21:00)",
            _ => "Unknown schedule pattern"
        };
    }

    #endregion

    /// <summary>
    /// Check which doctors are working on a specific date and time slot
    /// Used by hospital staff to filter available doctors for assignment
    /// </summary>
    public override async Task<CheckDoctorsWorkingSlotResponse> CheckDoctorsWorkingSlot(
        CheckDoctorsWorkingSlotRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogInformation(
                "[ScheduleGrpcService] CheckDoctorsWorkingSlot called - DoctorIds: {DoctorIds}, Date: {Date}, TimeSlot: {TimeSlot}",
                string.Join(", ", request.DoctorIds), request.Date, request.AppointmentTimeId);

            // Validate date
            if (!DateOnly.TryParse(request.Date, out var date))
            {
                return new CheckDoctorsWorkingSlotResponse
                {
                    Success = false,
                    Message = "Invalid date format. Expected YYYY-MM-DD"
                };
            }

            // Parse appointment time ID to enum
            if (!System.Enum.TryParse<BookingCare.Shared.Common.Enums.AppointmentTime>(request.AppointmentTimeId, out var appointmentTimeEnum))
            {
                return new CheckDoctorsWorkingSlotResponse
                {
                    Success = false,
                    Message = "Invalid appointment time ID format"
                };
            }

            // Parse doctor IDs
            var doctorIds = new List<Guid>();
            foreach (var idStr in request.DoctorIds)
            {
                if (Guid.TryParse(idStr, out var doctorId))
                {
                    doctorIds.Add(doctorId);
                }
            }

            if (!doctorIds.Any())
            {
                return new CheckDoctorsWorkingSlotResponse
                {
                    Success = true,
                    Message = "No valid doctor IDs provided"
                };
            }

            var response = new CheckDoctorsWorkingSlotResponse
            {
                Success = true,
                Message = "Doctor working statuses retrieved successfully"
            };

            // Check each doctor's schedule for the given date and time slot
            foreach (var doctorId in doctorIds)
            {
                var isWorking = await CheckDoctorWorkingSlotAsync(doctorId, date, appointmentTimeEnum);
                response.DoctorStatuses.Add(new DoctorWorkingStatus
                {
                    DoctorId = doctorId.ToString(),
                    IsWorking = isWorking
                });
            }

            _logger.LogInformation(
                "[ScheduleGrpcService] Returning working status for {Count} doctors",
                response.DoctorStatuses.Count);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ScheduleGrpcService] Error in CheckDoctorsWorkingSlot");
            return new CheckDoctorsWorkingSlotResponse
            {
                Success = false,
                Message = "An error occurred while checking doctor working slots"
            };
        }
    }

    /// <summary>
    /// Check if a doctor is working on a specific date and time slot
    /// Also checks if the slot is being held by another user (to avoid booking conflicts)
    /// </summary>
    private async Task<bool> CheckDoctorWorkingSlotAsync(
        Guid doctorId,
        DateOnly date,
        BookingCare.Shared.Common.Enums.AppointmentTime appointmentTime)
    {
        try
        {
            // Get doctor's schedule for the date
            var schedule = await _scheduleService.GetDoctorDailyScheduleAsync(doctorId, date);

            if (schedule == null)
            {
                // No schedule means doctor is not working on this date
                return false;
            }

            // Check if the appointment time is in the doctor's schedule patterns
            var isInPattern = schedule.SchedulePatterns
                .SelectMany(pattern => GetAppointmentTimesForPattern(pattern))
                .Contains(appointmentTime);

            if (!isInPattern)
            {
                return false;
            }

            // Check for exceptions (day off, specific slot blocked, etc.)
            var exceptions = await _scheduleService.GetDoctorExceptionsAsync(doctorId, date);

            // Check if there's an exception that blocks this specific slot
            var hasBlockingException = exceptions.Any(e =>
                e.AppointmentTime == appointmentTime && !e.IsAvailable);

            // Check if there's a full day off exception
            var hasFullDayOff = exceptions.Any(e =>
                e.AppointmentTime == null && !e.IsAvailable);

            if (hasBlockingException || hasFullDayOff)
            {
                return false;
            }

            // Check if the slot is being held by another user (soft reservation)
            // Use Guid.Empty as currentUserId since this is a staff check - we want to see ALL held slots
            var isSlotHeld = await _holdSlotService.IsSlotHeldByOtherUserAsync(
                doctorId,
                date,
                appointmentTime,
                Guid.Empty);

            if (isSlotHeld)
            {
                _logger.LogDebug(
                    "[ScheduleGrpcService] Slot {AppointmentTime} for doctor {DoctorId} on {Date} is being held by another user",
                    appointmentTime, doctorId, date);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "[ScheduleGrpcService] Error checking working slot for doctor {DoctorId} on {Date}",
                doctorId, date);
            // In case of error, assume doctor is not working to be safe
            return false;
        }
    }
}