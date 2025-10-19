using BookingCare.Services.Appointment.Repositories;
using BookingCare.Services.Appointment.Protos;
using Grpc.Core;

namespace BookingCare.Services.Appointment.Services.Grpc;

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
}