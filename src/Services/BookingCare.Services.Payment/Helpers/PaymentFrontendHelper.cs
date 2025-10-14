using BookingCare.Shared.Common.AppRouting;
using BookingCare.Services.Appointment.Protos; // Add this import

namespace BookingCare.Services.Payment.Helpers;

/// <summary>
/// Helper class for payment-related frontend operations
/// </summary>
public static class PaymentFrontendHelper
{
    /// <summary>
    /// Build frontend redirect URL for appointment payment result
    /// </summary>
    /// <param name="frontendOptions">Frontend configuration options</param>
    /// <param name="appointmentId">Appointment ID</param>
    /// <param name="success">Whether payment was successful</param>
    /// <returns>Frontend URL to redirect to</returns>
    public static string BuildAppointmentRedirectUrl(FrontendOptions frontendOptions, Guid appointmentId, bool success)
    {
        var baseUrl = frontendOptions.Client.BaseUrl.TrimEnd('/');

        if (success)
        {
            // Success: redirect to booking page with step 4 (completed)
            return $"{baseUrl}/booking/confirmation/{appointmentId}";
        }
        else
        {
            // Failed: redirect to booking page with error parameter
            return $"{baseUrl}/booking/confirmation/{appointmentId}?error=payment_failed";
        }
    }

    /// <summary>
    /// Build frontend redirect URL to doctor booking page when payment fails
    /// </summary>
    /// <param name="frontendOptions">Frontend configuration options</param>
    /// <param name="doctorId">Doctor ID to redirect to</param>
    /// <returns>Frontend URL to redirect to doctor's booking page</returns>
    public static string BuildDoctorBookingRedirectUrl(FrontendOptions frontendOptions, Guid doctorId)
    {
        var baseUrl = frontendOptions.Client.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/booking/{doctorId}";
    }

    /// <summary>
    /// Get doctor ID from appointment using gRPC call
    /// </summary>
    /// <param name="appointmentClient">Appointment gRPC client</param>
    /// <param name="appointmentId">Appointment ID</param>
    /// <returns>Doctor ID if found, null otherwise</returns>
    public static async Task<Guid?> GetDoctorIdFromAppointmentAsync(
        AppointmentService.AppointmentServiceClient appointmentClient,
        Guid appointmentId)
    {
        try
        {
            var request = new GetDoctorIdByAppointmentIdRequest
            {
                AppointmentId = appointmentId.ToString()
            };

            var response = await appointmentClient.GetDoctorIdByAppointmentIdAsync(request);

            if (response.Success && !string.IsNullOrEmpty(response.DoctorId))
            {
                if (Guid.TryParse(response.DoctorId, out var doctorId))
                {
                    return doctorId;
                }
            }

            return null;
        }
        catch (Grpc.Core.RpcException rpcEx)
        {
            // Log specific gRPC errors for troubleshooting
            var status = rpcEx.StatusCode;
            var detail = rpcEx.Status.Detail;

            // Don't throw - this is a helper method that should fail gracefully
            // The calling method will handle fallback to original error page
            System.Diagnostics.Debug.WriteLine($"[PaymentFrontendHelper] gRPC call failed - Status: {status}, Detail: {detail}, AppointmentId: {appointmentId}");
            return null;
        }
        catch (Exception ex)
        {
            // Log any other unexpected errors
            System.Diagnostics.Debug.WriteLine($"[PaymentFrontendHelper] Unexpected error in gRPC call: {ex.Message}, AppointmentId: {appointmentId}");
            return null;
        }
    }

    /// <summary>
    /// Check if payment is for an appointment and needs frontend redirect
    /// </summary>
    /// <param name="appointmentId">Appointment ID from payment</param>
    /// <returns>True if payment is for appointment and should redirect</returns>
    public static bool ShouldRedirectToFrontend(Guid? appointmentId)
    {
        return appointmentId.HasValue && appointmentId.Value != Guid.Empty;
    }

    /// <summary>
    /// Build frontend redirect URL with custom parameters
    /// </summary>
    /// <param name="frontendOptions">Frontend configuration options</param>
    /// <param name="appointmentId">Appointment ID</param>
    /// <param name="success">Whether payment was successful</param>
    /// <param name="customStep">Custom step number (optional)</param>
    /// <param name="additionalParams">Additional query parameters (optional)</param>
    /// <returns>Frontend URL to redirect to</returns>
    public static string BuildCustomAppointmentRedirectUrl(
        FrontendOptions frontendOptions,
        Guid appointmentId,
        bool success,
        int? customStep = null,
        Dictionary<string, string>? additionalParams = null)
    {
        var baseUrl = frontendOptions.Client.BaseUrl.TrimEnd('/');
        var step = customStep ?? (success ? 4 : 3);

        var queryParams = new List<string> { $"currentStep={step}" };

        if (!success)
        {
            queryParams.Add("error=payment_failed");
        }

        // Add additional parameters if provided
        if (additionalParams?.Any() == true)
        {
            foreach (var param in additionalParams)
            {
                queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");
            }
        }

        var queryString = string.Join("&", queryParams);
        return $"{baseUrl}/booking/{appointmentId}?{queryString}";
    }
}