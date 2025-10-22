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
                    DoctorId = string.Empty
                };
            }

            var appointmentEntity = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointmentEntity == null)
            {
                _logger.LogWarning("[AppointmentGrpcService] Appointment not found: {AppointmentId}", appointmentId);
                return new GetDoctorIdByAppointmentIdResponse
                {
                    Success = false,
                    DoctorId = string.Empty
                };
            }

            var doctorId = appointmentEntity.DoctorId?.ToString() ?? string.Empty;
            _logger.LogInformation("[AppointmentGrpcService] Successfully retrieved doctorId {DoctorId} for appointment: {AppointmentId}",
                doctorId, appointmentId);

            return new GetDoctorIdByAppointmentIdResponse
            {
                Success = true,
                DoctorId = doctorId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentGrpcService] Error in GetDoctorIdByAppointmentId for ID: {AppointmentId}", request.AppointmentId);
            return new GetDoctorIdByAppointmentIdResponse
            {
                Success = false,
                DoctorId = string.Empty
            };
        }
    }

    /// <summary>
    /// Get appointment details by ID (for Payment service integration)
    /// </summary>
    public override async Task<GetAppointmentDetailsResponse> GetAppointmentDetails(GetAppointmentDetailsRequest request, ServerCallContext context)
    {
        try
        {
            _logger.LogInformation("[AppointmentGrpcService] GetAppointmentDetails called with ID: {AppointmentId}", request.AppointmentId);

            if (!Guid.TryParse(request.AppointmentId, out var appointmentId))
            {
                _logger.LogWarning("[AppointmentGrpcService] Invalid appointment ID format: {AppointmentId}", request.AppointmentId);
                return new GetAppointmentDetailsResponse
                {
                    Success = false,
                    Message = "Invalid appointment ID format"
                };
            }

            var appointmentEntity = await _appointmentRepository.GetAppointmentByIdAsync(appointmentId);
            if (appointmentEntity == null)
            {
                _logger.LogWarning("[AppointmentGrpcService] Appointment not found: {AppointmentId}", appointmentId);
                return new GetAppointmentDetailsResponse
                {
                    Success = false,
                    Message = "Appointment not found"
                };
            }

            _logger.LogInformation("[AppointmentGrpcService] Successfully retrieved appointment details for ID: {AppointmentId}", appointmentId);

            return new GetAppointmentDetailsResponse
            {
                Success = true,
                Message = "Appointment details retrieved successfully",
                Appointment = new AppointmentDetails
                {
                    AppointmentId = appointmentEntity.Id.ToString(),
                    AppointmentDate = appointmentEntity.AppointmentDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    AppointmentType = (int)appointmentEntity.AppointmentType,
                    DoctorId = appointmentEntity.DoctorId?.ToString() ?? string.Empty,
                    ServiceId = appointmentEntity.ServiceId?.ToString() ?? string.Empty,
                    HospitalId = appointmentEntity.HospitalId?.ToString() ?? string.Empty
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentGrpcService] Error in GetAppointmentDetails for ID: {AppointmentId}", request.AppointmentId);
            return new GetAppointmentDetailsResponse
            {
                Success = false,
                Message = "Internal server error occurred while retrieving appointment details"
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
