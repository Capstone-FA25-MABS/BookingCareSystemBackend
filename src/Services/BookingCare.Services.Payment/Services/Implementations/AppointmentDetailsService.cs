using BookingCare.Services.Appointment.Protos;
using BookingCare.Services.Payment.Services.Interfaces;
using BookingCare.Shared.Common.Extensions;
using Grpc.Core;

namespace BookingCare.Services.Payment.Services.Implementations;

/// <summary>
/// Service implementation for retrieving appointment details via gRPC
/// </summary>
public class AppointmentDetailsService : IAppointmentDetailsService
{
    private readonly BookingCare.Services.Appointment.Protos.AppointmentService.AppointmentServiceClient _appointmentClient;
    private readonly ILogger<AppointmentDetailsService> _logger;

    public AppointmentDetailsService(
        BookingCare.Services.Appointment.Protos.AppointmentService.AppointmentServiceClient appointmentClient,
        ILogger<AppointmentDetailsService> logger)
    {
        _appointmentClient = appointmentClient;
        _logger = logger;
    }

    /// <summary>
    /// Get appointment details by appointment ID
    /// </summary>
    /// <param name="appointmentId">The appointment ID</param>
    /// <returns>Appointment details or null if not found</returns>
    public async Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(Guid appointmentId)
    {
        try
        {

            _logger.LogDebug("Getting appointment details for ID: {AppointmentId}", appointmentId);

            var request = new GetAppointmentDetailsRequest
            {
                AppointmentId = appointmentId.ToString()
            };

            var response = await _appointmentClient.GetAppointmentDetailsAsync(request);

            if (!response.Success || response.Appointment == null)
            {
                _logger.LogWarning("Failed to get appointment details for ID: {AppointmentId}, Message: {Message}",
                    appointmentId, response.Message);
                return null;
            }

            // Parse appointment type enum value to string
            var appointmentTypeString = response.Appointment.AppointmentType switch
            {
                0 => "IN_PERSON",
                1 => "TELEHEALTH",
                _ => "UNKNOWN"
            };

            var appointmentDetails = new AppointmentDetailsDto
            {
                AppointmentId = appointmentId,
                AppointmentDate = DateTime.Parse(response.Appointment.AppointmentDate, System.Globalization.CultureInfo.InvariantCulture),
                AppointmentType = appointmentTypeString,
                DoctorId = !string.IsNullOrEmpty(response.Appointment.DoctorId)
                    ? Guid.Parse(response.Appointment.DoctorId)
                    : null,
                ServiceId = !string.IsNullOrEmpty(response.Appointment.ServiceId)
                    ? Guid.Parse(response.Appointment.ServiceId)
                    : null,
                HospitalId = !string.IsNullOrEmpty(response.Appointment.HospitalId)
                    ? Guid.Parse(response.Appointment.HospitalId)
                    : null
            };

            _logger.LogDebug("Successfully retrieved appointment details for ID: {AppointmentId}, Type: {AppointmentType}, Date: {AppointmentDate}",
                appointmentId, appointmentDetails.AppointmentType, appointmentDetails.AppointmentDate);

            return appointmentDetails;
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "gRPC error getting appointment details for ID: {AppointmentId}, Status: {Status}",
                appointmentId, ex.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointment details for ID: {AppointmentId}", appointmentId);
            return null;
        }
    }
}