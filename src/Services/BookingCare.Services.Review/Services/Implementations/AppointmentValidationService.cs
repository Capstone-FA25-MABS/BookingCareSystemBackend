using BookingCare.Services.Appointment.Protos;
using BookingCare.Services.Review.Models.DTOs;
using BookingCare.Services.Review.Services.Interfaces;
using BookingCare.Shared.Common.Services;
using Grpc.Core;

namespace BookingCare.Services.Review.Services.Implementations;

/// <summary>
/// Service for validating patient appointment history with Appointment service
/// </summary>
public class AppointmentValidationService : BaseService, IAppointmentValidationService
{
    private readonly AppointmentService.AppointmentServiceClient _appointmentClient;

    public AppointmentValidationService(
        AppointmentService.AppointmentServiceClient appointmentClient,
        ILogger<AppointmentValidationService> logger
    )
        : base(logger)
    {
        _appointmentClient = appointmentClient;
    }

    /// <summary>
    /// Checks if patient has completed appointment with specified doctor
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="doctorId">Doctor ID</param>
    /// <returns>Appointment history validation result</returns>
    public async Task<AppointmentHistoryValidationResult> HasCompletedAppointmentWithDoctorAsync(
        Guid patientId,
        Guid doctorId
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Validating appointment history - Patient: {PatientId}, Doctor: {DoctorId}",
                    null,
                    patientId,
                    doctorId
                );

                var request = new CheckPatientAppointmentHistoryRequest
                {
                    PatientId = patientId.ToString(),
                    DoctorId = doctorId.ToString(),
                    ServiceId =
                        string.Empty // Not checking service for doctor appointments
                    ,
                };

                try
                {
                    var response = await _appointmentClient.CheckPatientAppointmentHistoryAsync(
                        request
                    );
                    return ProcessAppointmentHistoryResponse(
                        response,
                        "doctor",
                        doctorId.ToString()
                    );
                }
                catch (RpcException ex)
                {
                    return HandleGrpcException(ex, "doctor", doctorId.ToString());
                }
            },
            "ValidateAppointmentHistoryWithDoctor"
        );
    }

    /// <summary>
    /// Checks if patient has completed appointment with specified service
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <param name="serviceId">Service ID</param>
    /// <returns>Appointment history validation result</returns>
    public async Task<AppointmentHistoryValidationResult> HasCompletedAppointmentWithServiceAsync(
        Guid patientId,
        Guid serviceId
    )
    {
        return await ExecuteWithErrorHandling(
            async () =>
            {
                LogInfo(
                    "Validating appointment history - Patient: {PatientId}, Service: {ServiceId}",
                    null,
                    patientId,
                    serviceId
                );

                var request = new CheckPatientAppointmentHistoryRequest
                {
                    PatientId = patientId.ToString(),
                    DoctorId = string.Empty, // Not checking doctor for service appointments
                    ServiceId = serviceId.ToString(),
                };

                try
                {
                    var response = await _appointmentClient.CheckPatientAppointmentHistoryAsync(
                        request
                    );
                    return ProcessAppointmentHistoryResponse(
                        response,
                        "service",
                        serviceId.ToString()
                    );
                }
                catch (RpcException ex)
                {
                    return HandleGrpcException(ex, "service", serviceId.ToString());
                }
            },
            "ValidateAppointmentHistoryWithService"
        );
    }

    /// <summary>
    /// Processes the response from Appointment service
    /// </summary>
    private AppointmentHistoryValidationResult ProcessAppointmentHistoryResponse(
        CheckPatientAppointmentHistoryResponse response,
        string targetType,
        string targetId
    )
    {
        var result = new AppointmentHistoryValidationResult
        {
            HasCompletedAppointment = response.HasCompletedAppointment,
            TotalCompletedAppointments = response.TotalCompletedAppointments,
        };

        // Parse last appointment date if provided - use culture-invariant parsing
        if (!string.IsNullOrEmpty(response.LastCompletedAppointmentDate) &&
            DateTime.TryParse(response.LastCompletedAppointmentDate,
              System.Globalization.CultureInfo.InvariantCulture,
         System.Globalization.DateTimeStyles.None,
         out var lastDate))
        {
            result.LastCompletedAppointmentDate = lastDate;
        }

        LogInfo(
            "Appointment history validation result - {TargetType} {TargetId}: HasCompleted={HasCompleted}, Total={Total}",
            null,
            targetType,
          targetId,
 result.HasCompletedAppointment,
         result.TotalCompletedAppointments
        );

        return result;
    }

    /// <summary>
    /// Handles gRPC exceptions and returns fallback response
    /// </summary>
    private AppointmentHistoryValidationResult HandleGrpcException(
        RpcException ex,
        string targetType,
        string targetId
    )
    {
        var errorMessage = ex.StatusCode switch
        {
            StatusCode.DeadlineExceeded => "Appointment service call timed out: {Status}",
            StatusCode.Unavailable => "Appointment service is unavailable: {Status}",
            _ => "gRPC call to Appointment service failed: {Status} - {Detail}",
        };

        if (ex.StatusCode == StatusCode.DeadlineExceeded || ex.StatusCode == StatusCode.Unavailable)
        {
            LogWarning(errorMessage, null, ex.StatusCode.ToString());
        }
        else
        {
            LogWarning(errorMessage, null, ex.StatusCode.ToString(), ex.Status.Detail);
        }

        LogWarning(
            "Appointment validation failed for {TargetType} {TargetId} - allowing review creation as fallback",
            null,
            targetType,
            targetId
        );

        // Return permissive result as fallback to avoid blocking reviews due to service issues
        return new AppointmentHistoryValidationResult
        {
            HasCompletedAppointment = true, // Allow review creation on service failure
            TotalCompletedAppointments = 0,
        };
    }
}
