using BookingCare.Shared.Common.AppRouting;

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