using BookingCare.Services.Appointment.Enums;
using BookingCare.Services.Appointment.Protos;
using BookingCare.Services.Appointment.Repositories;
using Grpc.Core;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// gRPC service implementation for Appointment operations
/// </summary>
public class AppointmentGrpcService : Protos.AppointmentService.AppointmentServiceBase
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<AppointmentGrpcService> _logger;

    // Log message constants
    private const string LogInvalidAppointmentIdFormat = "[AppointmentGrpcService] Invalid appointment ID format: {AppointmentId}";
    private const string LogAppointmentNotFound = "[AppointmentGrpcService] Appointment not found: {AppointmentId}";

    public AppointmentGrpcService(
        IAppointmentRepository appointmentRepository,
        ILogger<AppointmentGrpcService> logger
    )
    {
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get doctor ID from appointment ID (for Payment service integration)
    /// </summary>
    public override async Task<GetDoctorIdByAppointmentIdResponse> GetDoctorIdByAppointmentId(
        GetDoctorIdByAppointmentIdRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "[AppointmentGrpcService] GetDoctorIdByAppointmentId called with ID: {AppointmentId}",
                request.AppointmentId
            );

            if (!Guid.TryParse(request.AppointmentId, out var appointmentId))
            {
                _logger.LogWarning(
                    LogInvalidAppointmentIdFormat,
                    request.AppointmentId
                );
                return new GetDoctorIdByAppointmentIdResponse
                {
                    Success = false,
                    DoctorId = string.Empty,
                    PendingDoctorId = string.Empty,
                    AssignedDoctorId = string.Empty,
                    RescheduleToken = string.Empty,
                    HospitalId = string.Empty,
                    SpecialtyId = string.Empty,
                };
            }

            var appointmentEntity = await _appointmentRepository.GetAppointmentByIdAsync(
                appointmentId
            );
            if (appointmentEntity == null)
            {
                _logger.LogWarning(
                    LogAppointmentNotFound,
                    appointmentId
                );
                return new GetDoctorIdByAppointmentIdResponse
                {
                    Success = false,
                    DoctorId = string.Empty,
                    PendingDoctorId = string.Empty,
                    AssignedDoctorId = string.Empty,
                    RescheduleToken = string.Empty,
                    HospitalId = string.Empty,
                    SpecialtyId = string.Empty,
                };
            }

            var doctorId = appointmentEntity.DoctorId?.ToString() ?? string.Empty;
            var pendingDoctorId = appointmentEntity.PendingNewDoctorId?.ToString() ?? string.Empty;
            var assignedDoctorId = appointmentEntity.AssignedDoctorId?.ToString() ?? string.Empty;
            var rescheduleToken = appointmentEntity.RescheduleToken ?? string.Empty;
            var hospitalId = appointmentEntity.HospitalId?.ToString() ?? string.Empty;
            var specialtyId = appointmentEntity.SpecialtyId?.ToString() ?? string.Empty;

            _logger.LogInformation(
                "[AppointmentGrpcService] Successfully retrieved doctorId {DoctorId} for appointment: {AppointmentId}",
                doctorId,
                appointmentId
            );

            return new GetDoctorIdByAppointmentIdResponse
            {
                Success = true,
                DoctorId = doctorId,
                PendingDoctorId = pendingDoctorId,
                AssignedDoctorId = assignedDoctorId,
                RescheduleToken = rescheduleToken,
                HospitalId = hospitalId,
                SpecialtyId = specialtyId,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AppointmentGrpcService] Error in GetDoctorIdByAppointmentId for ID: {AppointmentId}",
                request.AppointmentId
            );
            return new GetDoctorIdByAppointmentIdResponse
            {
                Success = false,
                DoctorId = string.Empty,
                PendingDoctorId = string.Empty,
                AssignedDoctorId = string.Empty,
                RescheduleToken = string.Empty,
                HospitalId = string.Empty,
                SpecialtyId = string.Empty,
            };
        }
    }

    /// <summary>
    /// Confirm appointment after supplementary payment (for Payment service integration)
    /// Updates appointment status from CANCELLED to CONFIRMED
    /// </summary>
    public override async Task<ConfirmAppointmentResponse> ConfirmAppointment(
        ConfirmAppointmentRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "[AppointmentGrpcService] ConfirmAppointment called with ID: {AppointmentId}",
                request.AppointmentId
            );

            if (!Guid.TryParse(request.AppointmentId, out var appointmentId))
            {
                _logger.LogWarning(
                    LogInvalidAppointmentIdFormat,
                    request.AppointmentId
                );
                return new ConfirmAppointmentResponse
                {
                    Success = false,
                    Message = "Invalid appointment ID format",
                };
            }

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                _logger.LogWarning(
                    LogAppointmentNotFound,
                    appointmentId
                );
                return new ConfirmAppointmentResponse
                {
                    Success = false,
                    Message = "Appointment not found",
                };
            }

            // Check if there are pending doctor changes (from Option 3 - Scenario 2: Higher price)
            // Apply them before confirming
            var hasPendingChanges =
                appointment.PendingNewDoctorId.HasValue
                && appointment.PendingNewAppointmentDate.HasValue
                && appointment.PendingNewAppointmentTimeId.HasValue;

            if (hasPendingChanges)
            {
                _logger.LogInformation(
                    "[AppointmentGrpcService] Applying pending doctor changes for appointment: {AppointmentId}",
                    appointmentId
                );

                appointment.DoctorId = appointment.PendingNewDoctorId;
                appointment.AppointmentDate = appointment.PendingNewAppointmentDate!.Value;
                appointment.AppointmentTimeId = appointment.PendingNewAppointmentTimeId!.Value;

                _logger.LogInformation(
                    "[AppointmentGrpcService] Applied pending doctor change to {DoctorId} for appointment: {AppointmentId}",
                    appointment.DoctorId,
                    appointmentId
                );
            }

            // If it was staff-assigned (Option 2), clear soft reservation fields
            if (request.StaffAssigned)
            {
                appointment.DoctorId = appointment.AssignedDoctorId;
                _logger.LogInformation(
                    "[AppointmentGrpcService] Cleared soft reservation for staff-assigned doctor"
                );
            }

            // Clear pending fields after applying
            appointment.PendingNewDoctorId = null;
            appointment.PendingNewAppointmentDate = null;
            appointment.PendingNewAppointmentTimeId = null;
            appointment.AssignedDoctorId = null; // Clear soft reservation
            appointment.SoftReservedUntil = null;
            appointment.IsRescheduled = true;
            appointment.RescheduleToken = null; // Clear token after use
            appointment.RescheduleTokenExpiry = null;

            // Update appointment status to CONFIRMED
            appointment.Status = AppointmentStatus.CONFIRMED;

            var success = await _appointmentRepository.UpdateAppointmentAsync(appointment);
            if (!success)
            {
                _logger.LogError(
                    "[AppointmentGrpcService] Failed to update appointment status for ID: {AppointmentId}",
                    appointmentId
                );
                return new ConfirmAppointmentResponse
                {
                    Success = false,
                    Message = "Failed to update appointment status",
                };
            }

            var message = hasPendingChanges
                ? "Appointment confirmed successfully with new doctor applied"
                : "Appointment confirmed successfully";

            _logger.LogInformation(
                "[AppointmentGrpcService] Successfully confirmed appointment: {AppointmentId}",
                appointmentId
            );
            return new ConfirmAppointmentResponse { Success = true, Message = message };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AppointmentGrpcService] Error in ConfirmAppointment for ID: {AppointmentId}",
                request.AppointmentId
            );
            return new ConfirmAppointmentResponse
            {
                Success = false,
                Message = $"Internal error: {ex.Message}",
            };
        }
    }

    /// <summary>
    /// Get appointment details by ID (for Payment service integration)
    /// </summary>
    public override async Task<GetAppointmentDetailsResponse> GetAppointmentDetails(
        GetAppointmentDetailsRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "[AppointmentGrpcService] GetAppointmentDetails called with ID: {AppointmentId}",
                request.AppointmentId
            );

            if (!Guid.TryParse(request.AppointmentId, out var appointmentId))
            {
                _logger.LogWarning(
                    LogInvalidAppointmentIdFormat,
                    request.AppointmentId
                );
                return new GetAppointmentDetailsResponse
                {
                    Success = false,
                    Message = "Invalid appointment ID format",
                };
            }

            var appointmentEntity = await _appointmentRepository.GetAppointmentByIdAsync(
                appointmentId
            );
            if (appointmentEntity == null)
            {
                _logger.LogWarning(
                    LogAppointmentNotFound,
                    appointmentId
                );
                return new GetAppointmentDetailsResponse
                {
                    Success = false,
                    Message = "Appointment not found",
                };
            }

            _logger.LogInformation(
                "[AppointmentGrpcService] Successfully retrieved appointment details for ID: {AppointmentId}",
                appointmentId
            );

            return new GetAppointmentDetailsResponse
            {
                Success = true,
                Message = "Appointment details retrieved successfully",
                Appointment = new AppointmentDetails
                {
                    AppointmentId = appointmentEntity.Id.ToString(),
                    AppointmentDate = appointmentEntity.AppointmentDate.ToString(
                        "yyyy-MM-ddTHH:mm:ss"
                    ),
                    AppointmentType = (int)appointmentEntity.AppointmentType,
                    DoctorId = appointmentEntity.DoctorId?.ToString() ?? string.Empty,
                    ServiceId = appointmentEntity.ServiceId?.ToString() ?? string.Empty,
                    HospitalId = appointmentEntity.HospitalId?.ToString() ?? string.Empty,
                },
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AppointmentGrpcService] Error in GetAppointmentDetails for ID: {AppointmentId}",
                request.AppointmentId
            );
            return new GetAppointmentDetailsResponse
            {
                Success = false,
                Message = "Internal server error occurred while retrieving appointment details",
            };
        }
    }

    /// <summary>
    /// Checks which appointment time slots are already booked for a doctor on a specific date
    /// Returns slots with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    public override async Task<CheckBookedSlotsResponse> CheckBookedSlots(
        CheckBookedSlotsRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogDebug(
                "Checking booked slots for doctor {DoctorId} on {Date}",
                request.DoctorId,
                request.AppointmentDate
            );

            // Parse the GUID and date
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid doctor ID format")
                );
            }

            if (!DateTime.TryParse(request.AppointmentDate, out var appointmentDate))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid appointment date format")
                );
            }

            // Get all appointments for this doctor on this date with PENDING, CONFIRMED, or COMPLETED status
            var bookedSlots = await _appointmentRepository.GetBookedAppointmentTimesAsync(
                doctorId,
                DateOnly.FromDateTime(appointmentDate)
            );

            var response = new CheckBookedSlotsResponse();
            response.BookedAppointmentTimeIds.AddRange(bookedSlots.Select(slot => (int)slot));

            _logger.LogDebug(
                "Found {Count} booked slots for doctor {DoctorId} on {Date}",
                response.BookedAppointmentTimeIds.Count,
                request.DoctorId,
                request.AppointmentDate
            );

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking booked slots for doctor {DoctorId} on {Date}",
                request.DoctorId,
                request.AppointmentDate
            );
            throw new RpcException(
                new Status(StatusCode.Internal, "An error occurred while checking booked slots")
            );
        }
    }

    /// <summary>
    /// Checks which appointment time slots are already booked for a service medical on a specific date
    /// Returns slots with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    public override async Task<CheckBookedSlotsResponse> CheckServiceBookedSlots(
        CheckServiceBookedSlotsRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogDebug(
                "Checking booked slots for service {ServiceId} on {Date}",
                request.ServiceId,
                request.AppointmentDate
            );

            // Parse the GUID and date
            if (!Guid.TryParse(request.ServiceId, out var serviceId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid service ID format")
                );
            }

            if (!DateTime.TryParse(request.AppointmentDate, out var appointmentDate))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid appointment date format")
                );
            }

            // Get all appointments for this service on this date with PENDING, CONFIRMED, or COMPLETED status
            var bookedSlots = await _appointmentRepository.GetBookedAppointmentTimesByServiceAsync(
                serviceId,
                DateOnly.FromDateTime(appointmentDate)
            );

            var response = new CheckBookedSlotsResponse();
            response.BookedAppointmentTimeIds.AddRange(bookedSlots.Select(slot => (int)slot));

            _logger.LogDebug(
                "Found {Count} booked slots for service {ServiceId} on {Date}",
                response.BookedAppointmentTimeIds.Count,
                request.ServiceId,
                request.AppointmentDate
            );

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking booked slots for service {ServiceId} on {Date}",
                request.ServiceId,
                request.AppointmentDate
            );
            throw new RpcException(
                new Status(StatusCode.Internal, "An error occurred while checking booked slots")
            );
        }
    }

    /// <summary>
    /// Checks booked slots count for a specialty (hospital assigns doctor mode)
    /// Returns count of PENDING/CONFIRMED appointments per time slot
    /// </summary>
    public override async Task<CheckSpecialtyBookedSlotsResponse> CheckSpecialtyBookedSlots(
        CheckSpecialtyBookedSlotsRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogDebug(
                "Checking specialty booked slots for hospital {HospitalId}, specialty {SpecialtyId} on {Date}",
                request.HospitalId,
                request.SpecialtyId,
                request.AppointmentDate
            );

            // Parse the GUIDs and date
            if (!Guid.TryParse(request.HospitalId, out var hospitalId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid hospital ID format")
                );
            }

            if (!Guid.TryParse(request.SpecialtyId, out var specialtyId))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid specialty ID format")
                );
            }

            if (!DateTime.TryParse(request.AppointmentDate, out var appointmentDate))
            {
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid appointment date format")
                );
            }

            var appointmentType = (AppointmentType)request.AppointmentType;

            // Get booked counts per time slot for this specialty
            var bookedCounts = await _appointmentRepository.GetSpecialtyBookedSlotCountsAsync(
                hospitalId,
                specialtyId,
                DateOnly.FromDateTime(appointmentDate),
                appointmentType
            );

            var response = new CheckSpecialtyBookedSlotsResponse();
            foreach (var kvp in bookedCounts)
            {
                response.BookedSlots.Add(
                    new SpecialtySlotBookedCount
                    {
                        AppointmentTimeId = (int)kvp.Key,
                        BookedCount = kvp.Value,
                    }
                );
            }

            _logger.LogDebug(
                "Found {Count} time slots with bookings for specialty {SpecialtyId} on {Date}",
                response.BookedSlots.Count,
                request.SpecialtyId,
                request.AppointmentDate
            );

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking specialty booked slots for hospital {HospitalId}, specialty {SpecialtyId} on {Date}",
                request.HospitalId,
                request.SpecialtyId,
                request.AppointmentDate
            );
            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    "An error occurred while checking specialty booked slots"
                )
            );
        }
    }

    /// <summary>
    /// NEW: Check if patient has completed appointment history with doctor or service (for Review service validation)
    /// </summary>
    public override async Task<CheckPatientAppointmentHistoryResponse> CheckPatientAppointmentHistory(
        CheckPatientAppointmentHistoryRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "[AppointmentGrpcService] CheckPatientAppointmentHistory called - Patient: {PatientId}, Doctor: {DoctorId}, Service: {ServiceId}",
                request.PatientId,
                request.DoctorId ?? "null",
                request.ServiceId ?? "null"
            );

            // Validate patient ID
            if (!Guid.TryParse(request.PatientId, out var patientId))
            {
                _logger.LogWarning(
                    "[AppointmentGrpcService] Invalid patient ID format: {PatientId}",
                    request.PatientId
                );
                throw new RpcException(
                    new Status(StatusCode.InvalidArgument, "Invalid patient ID format")
                );
            }

            Guid? doctorId = null;
            Guid? serviceId = null;

            // Parse doctor ID if provided
            if (!string.IsNullOrEmpty(request.DoctorId))
            {
                if (!Guid.TryParse(request.DoctorId, out var parsedDoctorId))
                {
                    _logger.LogWarning(
                        "[AppointmentGrpcService] Invalid doctor ID format: {DoctorId}",
                        request.DoctorId
                    );
                    throw new RpcException(
                        new Status(StatusCode.InvalidArgument, "Invalid doctor ID format")
                    );
                }
                doctorId = parsedDoctorId;
            }

            // Parse service ID if provided
            if (!string.IsNullOrEmpty(request.ServiceId))
            {
                if (!Guid.TryParse(request.ServiceId, out var parsedServiceId))
                {
                    _logger.LogWarning(
                        "[AppointmentGrpcService] Invalid service ID format: {ServiceId}",
                        request.ServiceId
                    );
                    throw new RpcException(
                        new Status(StatusCode.InvalidArgument, "Invalid service ID format")
                    );
                }
                serviceId = parsedServiceId;
            }

            // Must specify either doctor or service, but not both
            if (
                (doctorId.HasValue && serviceId.HasValue)
                || (!doctorId.HasValue && !serviceId.HasValue)
            )
            {
                _logger.LogWarning(
                    "[AppointmentGrpcService] Must specify either doctor ID or service ID, but not both or neither"
                );
                throw new RpcException(
                    new Status(
                        StatusCode.InvalidArgument,
                        "Must specify either doctor ID or service ID, but not both"
                    )
                );
            }

            // Get completed appointments based on the target type
            var completedAppointments =
                await _appointmentRepository.GetCompletedAppointmentsByPatientAsync(
                    patientId,
                    doctorId,
                    serviceId
                );

            var totalCompleted = completedAppointments.Count;
            var hasCompleted = totalCompleted > 0;

            // Get the most recent completed appointment date
            var lastCompletedDate = completedAppointments
                .OrderByDescending(a => a.AppointmentDate)
                .FirstOrDefault()
                ?.AppointmentDate;

            var response = new CheckPatientAppointmentHistoryResponse
            {
                HasCompletedAppointment = hasCompleted,
                TotalCompletedAppointments = totalCompleted,
                LastCompletedAppointmentDate =
                    lastCompletedDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
            };

            var targetInfo = doctorId.HasValue ? $"doctor {doctorId}" : $"service {serviceId}";
            _logger.LogInformation(
                "[AppointmentGrpcService] Patient {PatientId} has {Count} completed appointments with {Target}",
                patientId,
                totalCompleted,
                targetInfo
            );

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[AppointmentGrpcService] Error in CheckPatientAppointmentHistory - Patient: {PatientId}, Doctor: {DoctorId}, Service: {ServiceId}",
                request.PatientId,
                request.DoctorId ?? "null",
                request.ServiceId ?? "null"
            );
            throw new RpcException(
                new Status(
                    StatusCode.Internal,
                    "An error occurred while checking patient appointment history"
                )
            );
        }
    }

    /// <summary>
    /// Validate that appointments are actually completed (for Payment service payout validation)
    /// </summary>
    public override async Task<ValidateCompletedAppointmentsResponse> ValidateCompletedAppointments(
        ValidateCompletedAppointmentsRequest request,
        ServerCallContext context
    )
    {
        try
        {
            _logger.LogInformation(
                "[AppointmentGrpcService] ValidateCompletedAppointments called with {Count} appointment IDs",
                request.AppointmentIds.Count
            );

            var response = new ValidateCompletedAppointmentsResponse();

            if (!request.AppointmentIds.Any())
            {
                return response;
            }

            // Parse appointment IDs
            var appointmentIds = new List<Guid>();
            foreach (var idStr in request.AppointmentIds)
            {
                if (Guid.TryParse(idStr, out var id))
                {
                    appointmentIds.Add(id);
                }
                else
                {
                    _logger.LogWarning(
                        LogInvalidAppointmentIdFormat,
                        idStr
                    );
                }
            }

            // Get appointments
            var appointments = await _appointmentRepository.GetAppointmentsByIdsAsync(
                appointmentIds
            );

            // Separate into completed and non-completed
            foreach (var appointment in appointments)
            {
                if (appointment.Status == AppointmentStatus.COMPLETED)
                {
                    response.CompletedAppointmentIds.Add(appointment.Id.ToString());
                }
                else
                {
                    response.NonCompletedAppointmentIds.Add(appointment.Id.ToString());
                }
            }

            // Check for missing appointments (IDs that weren't found in database)
            var foundIds = appointments.Select(a => a.Id).ToHashSet();
            var missingIds = appointmentIds.Where(id => !foundIds.Contains(id));
            foreach (var missingId in missingIds)
            {
                response.NonCompletedAppointmentIds.Add(missingId.ToString());
                _logger.LogWarning(
                    LogAppointmentNotFound,
                    missingId
                );
            }

            response.TotalCompleted = response.CompletedAppointmentIds.Count;
            response.TotalNonCompleted = response.NonCompletedAppointmentIds.Count;

            _logger.LogInformation(
                "[AppointmentGrpcService] Validation complete: {Completed} completed, {NonCompleted} non-completed",
                response.TotalCompleted,
                response.TotalNonCompleted
            );

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentGrpcService] Error in ValidateCompletedAppointments");
            throw new RpcException(
                new Status(StatusCode.Internal, "An error occurred while validating appointments")
            );
        }
    }
}
