using System.Globalization;

namespace BookingCare.Services.Notification.Helpers;

/// <summary>
/// Helper class for date/time formatting in notifications
/// </summary>
public static class DateTimeHelper
{
    private const string VietnamTimeZoneId = "SE Asia Standard Time";

    /// <summary>
    /// Format date in Vietnamese format: "ngày dd/MM/yyyy"
    /// </summary>
    public static string FormatAppointmentDateVi(DateTime date)
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(VietnamTimeZoneId);
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(date, vietnamTimeZone);
        return vietnamTime.ToString("'ngày' dd/MM/yyyy");
    }

    /// <summary>
    /// Format date in English format: "MMMM dd, yyyy"
    /// </summary>
    public static string FormatAppointmentDateEn(DateTime date)
    {
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById(VietnamTimeZoneId);
        var vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(date, vietnamTimeZone);
        return vietnamTime.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
    }
}
