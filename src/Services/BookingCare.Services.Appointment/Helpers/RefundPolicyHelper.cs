using BookingCare.Shared.Common.Enums;

namespace BookingCare.Services.Appointment.Helpers;

/// <summary>
/// Helper class for calculating refund percentage based on cancellation time
/// </summary>
public static class RefundPolicyHelper
{
    /// <summary>
    /// Parse AppointmentTimeId enum to get start time (HH:mm format)
    /// Example: AT_08_00_09_00 -> returns TimeOnly(08, 00)
    /// </summary>
    private static TimeOnly? ParseAppointmentStartTime(AppointmentTime appointmentTimeId)
    {
        try
        {
            // Format: AT_08_00_09_00 (8:00-9:00)
            var timeString = appointmentTimeId.ToString();
            var parts = timeString.Split('_');

            if (parts.Length >= 3 && parts[0] == "AT")
            {
                if (int.TryParse(parts[1], out int hour) && int.TryParse(parts[2], out int minute))
                {
                    return new TimeOnly(hour, minute);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Combine AppointmentDate (date only) with AppointmentTimeId (time) to get full DateTime
    /// </summary>
    private static DateTime GetFullAppointmentDateTime(DateTime appointmentDate, AppointmentTime appointmentTimeId)
    {
        var startTime = ParseAppointmentStartTime(appointmentTimeId);

        if (startTime.HasValue)
        {
            // Combine date with time
            return appointmentDate.Date.Add(startTime.Value.ToTimeSpan());
        }

        // Fallback: use date only (assume start of day)
        return appointmentDate.Date;
    }
    /// <summary>
    /// Calculate refund percentage based on time between cancellation and appointment
    /// Policy: 
    /// - Staff/Hospital cancel: Always 100% refund (hospital's fault)
    /// - Patient cancel with >= 24 hours before: 100% refund
    /// - Patient cancel with 12-24 hours before: 50% refund
    /// - Patient cancel with < 12 hours before: 0% refund (No refund)
    /// </summary>
    /// <param name="appointmentDate">The appointment date and time</param>
    /// <param name="cancellationDate">The cancellation date and time (defaults to now)</param>
    /// <param name="isStaffCancellation">True if cancelled by staff/hospital, false if cancelled by patient</param>
    /// <returns>Refund percentage (0-100)</returns>
    public static decimal CalculateRefundPercentage(DateTime appointmentDate, DateTime? cancellationDate = null, bool isStaffCancellation = false)
    {
        // If cancelled by staff/hospital, always full refund (hospital's responsibility)
        if (isStaffCancellation)
        {
            return 100m; // Full refund for hospital cancellation
        }

        // Patient cancellation - apply time-based policy
        var cancelTime = cancellationDate ?? DateTime.UtcNow;
        var hoursUntilAppointment = (appointmentDate - cancelTime).TotalHours;

        if (hoursUntilAppointment >= 24)
        {
            return 100m; // Full refund
        }
        else if (hoursUntilAppointment >= 12)
        {
            return 50m; // Half refund
        }
        else
        {
            return 0m; // No refund
        }
    }

    /// <summary>
    /// Get refund policy message based on hours until appointment
    /// </summary>
    /// <param name="hoursUntilAppointment">Hours until appointment</param>
    /// <returns>Policy message</returns>
    public static string GetRefundPolicyMessage(double hoursUntilAppointment)
    {
        if (hoursUntilAppointment >= 24)
        {
            return "Bạn sẽ được hoàn lại 100% chi phí";
        }
        else if (hoursUntilAppointment >= 12)
        {
            return "Bạn sẽ được hoàn lại 50% chi phí";
        }
        else if (hoursUntilAppointment >= 0)
        {
            return "Bạn sẽ không được hoàn lại chi phí";
        }
        else
        {
            return "Không thể hủy lịch hẹn đã qua";
        }
    }

    /// <summary>
    /// Check if cancellation is allowed (appointment must be in the future)
    /// </summary>
    /// <param name="appointmentDate">The appointment date and time</param>
    /// <param name="cancellationDate">The cancellation date and time (defaults to now)</param>
    /// <returns>True if cancellation is allowed</returns>
    public static bool IsCancellationAllowed(DateTime appointmentDate, DateTime? cancellationDate = null)
    {
        var cancelTime = cancellationDate ?? DateTime.UtcNow;
        return appointmentDate > cancelTime;
    }

    /// <summary>
    /// Check if reschedule is allowed (must be at least 24 hours before appointment)
    /// More strict than cancellation - requires minimum 1 day notice
    /// </summary>
    /// <param name="appointmentDate">The appointment date (date only)</param>
    /// <param name="appointmentTimeId">The appointment time slot</param>
    /// <param name="requestTime">The reschedule request time (defaults to now)</param>
    /// <returns>True if reschedule is allowed</returns>
    public static bool IsRescheduleAllowed(DateTime appointmentDate, AppointmentTime appointmentTimeId, DateTime? requestTime = null)
    {
        var checkTime = requestTime ?? DateTime.UtcNow;
        var fullAppointmentDateTime = GetFullAppointmentDateTime(appointmentDate, appointmentTimeId);
        var hoursUntilAppointment = (fullAppointmentDateTime - checkTime).TotalHours;

        // Must be at least 24 hours (1 day) before appointment
        return hoursUntilAppointment >= 24;
    }

    /// <summary>
    /// Check if reschedule is allowed (fallback method - uses date only)
    /// WARNING: This is less accurate. Use overload with AppointmentTime when possible.
    /// </summary>
    /// <param name="appointmentDate">The appointment date and time</param>
    /// <param name="requestTime">The reschedule request time (defaults to now)</param>
    /// <returns>True if reschedule is allowed</returns>
    [Obsolete("Use overload with AppointmentTime parameter for accurate validation")]
    public static bool IsRescheduleAllowed(DateTime appointmentDate, DateTime? requestTime = null)
    {
        var checkTime = requestTime ?? DateTime.UtcNow;
        var hoursUntilAppointment = (appointmentDate - checkTime).TotalHours;

        // Must be at least 24 hours (1 day) before appointment
        return hoursUntilAppointment >= 24;
    }

    /// <summary>
    /// Get reschedule policy message
    /// </summary>
    /// <param name="appointmentDate">The appointment date (date only)</param>
    /// <param name="appointmentTimeId">The appointment time slot</param>
    /// <param name="requestTime">The reschedule request time (defaults to now)</param>
    /// <returns>Policy message</returns>
    public static string GetReschedulePolicyMessage(DateTime appointmentDate, AppointmentTime appointmentTimeId, DateTime? requestTime = null)
    {
        var checkTime = requestTime ?? DateTime.UtcNow;
        var fullAppointmentDateTime = GetFullAppointmentDateTime(appointmentDate, appointmentTimeId);
        var hoursUntilAppointment = (fullAppointmentDateTime - checkTime).TotalHours;

        if (hoursUntilAppointment >= 24)
        {
            return "Bạn có thể đổi lịch hẹn này";
        }
        else if (hoursUntilAppointment > 0)
        {
            return $"Chỉ có thể đổi lịch trước ít nhất 24 giờ. Còn {hoursUntilAppointment:F1} giờ nữa đến lịch hẹn.";
        }
        else
        {
            return "Không thể đổi lịch hẹn đã qua";
        }
    }

    /// <summary>
    /// Get reschedule policy message (fallback method - uses date only)
    /// WARNING: This is less accurate. Use overload with AppointmentTime when possible.
    /// </summary>
    /// <param name="appointmentDate">The appointment date and time</param>
    /// <param name="requestTime">The reschedule request time (defaults to now)</param>
    /// <returns>Policy message</returns>
    [Obsolete("Use overload with AppointmentTime parameter for accurate validation")]
    public static string GetReschedulePolicyMessage(DateTime appointmentDate, DateTime? requestTime = null)
    {
        var checkTime = requestTime ?? DateTime.UtcNow;
        var hoursUntilAppointment = (appointmentDate - checkTime).TotalHours;

        if (hoursUntilAppointment >= 24)
        {
            return "Bạn có thể đổi lịch hẹn này";
        }
        else if (hoursUntilAppointment > 0)
        {
            return $"Chỉ có thể đổi lịch trước ít nhất 24 giờ. Còn {hoursUntilAppointment:F1} giờ nữa đến lịch hẹn.";
        }
        else
        {
            return "Không thể đổi lịch hẹn đã qua";
        }
    }

    /// <summary>
    /// Get detailed refund info for display
    /// </summary>
    public static RefundInfo GetRefundInfo(DateTime appointmentDate, DateTime? cancellationDate = null, bool isStaffCancellation = false)
    {
        var cancelTime = cancellationDate ?? DateTime.UtcNow;
        var hoursUntilAppointment = (appointmentDate - cancelTime).TotalHours;
        var refundPercentage = CalculateRefundPercentage(appointmentDate, cancellationDate, isStaffCancellation);
        var isAllowed = IsCancellationAllowed(appointmentDate, cancellationDate);

        return new RefundInfo
        {
            RefundPercentage = refundPercentage,
            HoursUntilAppointment = hoursUntilAppointment,
            IsAllowed = isAllowed,
            PolicyMessage = isStaffCancellation
                ? "Bệnh viện hủy lịch hẹn - Bạn sẽ được hoàn lại 100% chi phí"
                : GetRefundPolicyMessage(hoursUntilAppointment)
        };
    }
}

/// <summary>
/// Refund information model
/// </summary>
public class RefundInfo
{
    public decimal RefundPercentage { get; set; }
    public double HoursUntilAppointment { get; set; }
    public bool IsAllowed { get; set; }
    public string PolicyMessage { get; set; } = string.Empty;
}

