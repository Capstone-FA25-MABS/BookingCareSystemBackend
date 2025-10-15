using BookingCare.Shared.Common.Enums;

namespace BookingCare.Shared.Common.Extensions;

/// <summary>
/// Extension methods for AppointmentTime enum
/// </summary>
public static class AppointmentTimeExtensions
{
    /// <summary>
    /// Formats AppointmentTime enum to readable time range string for display
    /// Uses the enum naming convention to extract start and end times
    /// </summary>
    /// <param name="appointmentTime">The appointment time enum</param>
    /// <returns>Formatted time range string (e.g., "08:00 - 08:30")</returns>
    public static string ToDisplayString(this AppointmentTime appointmentTime)
    {
        // Parse enum name to derive start/end time
        var name = appointmentTime.ToString();

        // Expected format: AT_HH_MM_HH_MM
        var parts = name.Split('_');

        try
        {
            if (parts.Length >= 5)
            {
                // parts: ["AT", "HH", "MM", "HH", "MM"]
                var startHour = parts[1];
                var startMinute = parts[2];
                var endHour = parts[3];
                var endMinute = parts[4];

                return $"{startHour}:{startMinute} - {endHour}:{endMinute}";
            }
        }
        catch
        {
            // Fall through to fallback
        }

        // Fallback: return enum name if parsing fails
        return appointmentTime.ToString();
    }

    /// <summary>
    /// Gets the start time from AppointmentTime enum
    /// </summary>
    /// <param name="appointmentTime">The appointment time enum</param>
    /// <returns>Start time string (e.g., "08:00")</returns>
    public static string GetStartTime(this AppointmentTime appointmentTime)
    {
        var name = appointmentTime.ToString();
        var parts = name.Split('_');

        try
        {
            if (parts.Length >= 3)
            {
                return $"{parts[1]}:{parts[2]}";
            }
        }
        catch
        {
            // Fall through to fallback
        }

        return "Unknown";
    }

    /// <summary>
    /// Gets the end time from AppointmentTime enum
    /// </summary>
    /// <param name="appointmentTime">The appointment time enum</param>
    /// <returns>End time string (e.g., "08:30")</returns>
    public static string GetEndTime(this AppointmentTime appointmentTime)
    {
        var name = appointmentTime.ToString();
        var parts = name.Split('_');

        try
        {
            if (parts.Length >= 5)
            {
                return $"{parts[3]}:{parts[4]}";
            }
        }
        catch
        {
            // Fall through to fallback
        }

        return "Unknown";
    }

    /// <summary>
    /// Gets the duration in minutes for the appointment time slot
    /// </summary>
    /// <param name="appointmentTime">The appointment time enum</param>
    /// <returns>Duration in minutes (30 or 60)</returns>
    public static int GetDurationInMinutes(this AppointmentTime appointmentTime)
    {
        var startTime = appointmentTime.GetStartTime();
        var endTime = appointmentTime.GetEndTime();

        if (TimeOnly.TryParse(startTime, out var start) && TimeOnly.TryParse(endTime, out var end))
        {
            return (int)(end - start).TotalMinutes;
        }

        // Default to 30 minutes if parsing fails
        return 30;
    }
}