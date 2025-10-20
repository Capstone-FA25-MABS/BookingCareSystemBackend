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
