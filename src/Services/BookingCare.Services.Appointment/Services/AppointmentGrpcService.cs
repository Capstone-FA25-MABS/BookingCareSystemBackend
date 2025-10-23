using BookingCare.Services.Appointment.Protos;
using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Enums;
using Grpc.Core;

namespace BookingCare.Services.Appointment.Services;

/// <summary>
/// gRPC service implementation for Appointment operations
/// </summary>
public class AppointmentGrpcService : Protos.AppointmentService.AppointmentServiceBase
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<AppointmentGrpcService> _logger;

    public AppointmentGrpcService(
        IAppointmentRepository appointmentRepository,
        ILogger<AppointmentGrpcService> logger)
    {
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get doctor ID from appointment ID (for Payment service integration)
    /// </summary>
    public override async Task<GetDoctorIdByAppointmentIdResponse> GetDoctorIdByAppointmentId(GetDoctorIdByAppointmentIdRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[AppointmentGrpcService] GetDoctorIdByAppointmentId called with ID: {AppointmentId}", request.AppointmentId);

            if (!Guid.TryParse(request.AppointmentId, out var appointmentId))
            {
                _logger.LogWarning("[AppointmentGrpcService] Invalid appointment ID format: {AppointmentId}", request.AppointmentId);
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

            var appointmentEntity = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointmentEntity == null)
            {
                _logger.LogWarning("[AppointmentGrpcService] Appointment not found: {AppointmentId}", appointmentId);
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

            _logger.LogInformation("[AppointmentGrpcService] Successfully retrieved doctorId {DoctorId} for appointment: {AppointmentId}",
                doctorId, appointmentId);

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
            _logger.LogError(ex, "[AppointmentGrpcService] Error in GetDoctorIdByAppointmentId for ID: {AppointmentId}", request.AppointmentId);
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
    public override async Task<ConfirmAppointmentResponse> ConfirmAppointment(ConfirmAppointmentRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[AppointmentGrpcService] ConfirmAppointment called with ID: {AppointmentId}", request.AppointmentId);

            if (!Guid.TryParse(request.AppointmentId, out var appointmentId))
            {
                _logger.LogWarning("[AppointmentGrpcService] Invalid appointment ID format: {AppointmentId}", request.AppointmentId);
                return new ConfirmAppointmentResponse
                {
                    Success = false,
                    Message = "Invalid appointment ID format"
                };
            }

            var appointment = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointment == null)
            {
                _logger.LogWarning("[AppointmentGrpcService] Appointment not found: {AppointmentId}", appointmentId);
                return new ConfirmAppointmentResponse
                {
                    Success = false,
                    Message = "Appointment not found"
                };
            }

            // Check if there are pending doctor changes (from Option 3 - Scenario 2: Higher price)
            // Apply them before confirming
            var hasPendingChanges = appointment.PendingNewDoctorId.HasValue &&
                                   appointment.PendingNewAppointmentDate.HasValue &&
                                   appointment.PendingNewAppointmentTimeId.HasValue;

            if (hasPendingChanges)
            {
                _logger.LogInformation("[AppointmentGrpcService] Applying pending doctor changes for appointment: {AppointmentId}", appointmentId);

                appointment.DoctorId = appointment.PendingNewDoctorId;
                appointment.AppointmentDate = appointment.PendingNewAppointmentDate!.Value;
                appointment.AppointmentTimeId = appointment.PendingNewAppointmentTimeId!.Value;

                _logger.LogInformation("[AppointmentGrpcService] Applied pending doctor change to {DoctorId} for appointment: {AppointmentId}",
                    appointment.DoctorId, appointmentId);
            }

            // If it was staff-assigned (Option 2), clear soft reservation fields
            if (request.StaffAssigned)
            {
                appointment.DoctorId = appointment.AssignedDoctorId;
                _logger.LogInformation("[AppointmentGrpcService] Cleared soft reservation for staff-assigned doctor");
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
                _logger.LogError("[AppointmentGrpcService] Failed to update appointment status for ID: {AppointmentId}", appointmentId);
                return new ConfirmAppointmentResponse
                {
                    Success = false,
                    Message = "Failed to update appointment status"
                };
            }

            var message = hasPendingChanges
                ? "Appointment confirmed successfully with new doctor applied"
                : "Appointment confirmed successfully";

            _logger.LogInformation("[AppointmentGrpcService] Successfully confirmed appointment: {AppointmentId}", appointmentId);
            return new ConfirmAppointmentResponse
            {
                Success = true,
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentGrpcService] Error in ConfirmAppointment for ID: {AppointmentId}", request.AppointmentId);
            return new ConfirmAppointmentResponse
            {
                Success = false,
                Message = $"Internal error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks which appointment time slots are already booked for a doctor on a specific date
    /// Returns slots with status PENDING, CONFIRMED, or COMPLETED
    /// </summary>
    public override async Task<CheckBookedSlotsResponse> CheckBookedSlots(
        CheckBookedSlotsRequest request,
        ServerCallContext context)
    {
        try
        {
            _logger.LogDebug("Checking booked slots for doctor {DoctorId} on {Date}",
                request.DoctorId, request.AppointmentDate);

            // Parse the GUID and date
            if (!Guid.TryParse(request.DoctorId, out var doctorId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid doctor ID format"));
            }

            if (!DateTime.TryParse(request.AppointmentDate, out var appointmentDate))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid appointment date format"));
            }

            // Get all appointments for this doctor on this date with PENDING, CONFIRMED, or COMPLETED status
            var bookedSlots = await _appointmentRepository.GetBookedAppointmentTimesAsync(
                doctorId,
                DateOnly.FromDateTime(appointmentDate));

            var response = new CheckBookedSlotsResponse();
            response.BookedAppointmentTimeIds.AddRange(bookedSlots.Select(slot => (int)slot));

            _logger.LogDebug("Found {Count} booked slots for doctor {DoctorId} on {Date}",
                response.BookedAppointmentTimeIds.Count, request.DoctorId, request.AppointmentDate);

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking booked slots for doctor {DoctorId} on {Date}",
                request.DoctorId, request.AppointmentDate);
            throw new RpcException(new Status(StatusCode.Internal, "An error occurred while checking booked slots"));
        }
    }
}
