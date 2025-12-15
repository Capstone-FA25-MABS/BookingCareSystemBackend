using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Helpers;

/// <summary>
/// Helper class for appointment time-related operations
/// </summary>
public static class AppointmentTimeHelper
{
    /// <summary>
    /// Get display string for appointment time slot
    /// </summary>
    /// <param name="appointmentTimeId">The appointment time enum value</param>
    /// <returns>Formatted time range string (e.g., "08:00 - 08:30")</returns>
    public static string GetAppointmentTimeDisplay(AppointmentTime appointmentTimeId)
    {
        return appointmentTimeId switch
        {
            AppointmentTime.AT_08_00_08_30 => "08:00 - 08:30",
            AppointmentTime.AT_08_30_09_00 => "08:30 - 09:00",
            AppointmentTime.AT_09_00_09_30 => "09:00 - 09:30",
            AppointmentTime.AT_09_30_10_00 => "09:30 - 10:00",
            AppointmentTime.AT_10_00_10_30 => "10:00 - 10:30",
            AppointmentTime.AT_10_30_11_00 => "10:30 - 11:00",
            AppointmentTime.AT_11_00_11_30 => "11:00 - 11:30",
            AppointmentTime.AT_11_30_12_00 => "11:30 - 12:00",
            AppointmentTime.AT_13_00_13_30 => "13:00 - 13:30",
            AppointmentTime.AT_13_30_14_00 => "13:30 - 14:00",
            AppointmentTime.AT_14_00_14_30 => "14:00 - 14:30",
            AppointmentTime.AT_14_30_15_00 => "14:30 - 15:00",
            AppointmentTime.AT_15_00_15_30 => "15:00 - 15:30",
            AppointmentTime.AT_15_30_16_00 => "15:30 - 16:00",
            AppointmentTime.AT_16_00_16_30 => "16:00 - 16:30",
            AppointmentTime.AT_16_30_17_00 => "16:30 - 17:00",
            _ => "Không xác định"
        };
    }
}
