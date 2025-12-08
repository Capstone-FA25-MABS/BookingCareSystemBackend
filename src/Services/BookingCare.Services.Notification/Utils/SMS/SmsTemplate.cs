using BookingCare.Shared.EventBus.Events;

namespace BookingCare.Services.Notification.Utils.SMS;

/// <summary>
/// SMS message templates for various notification scenarios
/// SMS messages should be concise to minimize costs
/// </summary>
public static class SmsTemplate
{
    /// <summary>
    /// Build SMS content for appointment cancellation refund (patient has bank account)
    /// </summary>
    public static string BuildRefundSmsWithBankAccount(
        DateTime appointmentDate,
        decimal refundAmount)
    {
        // Keep message short and without Vietnamese diacritics to ensure compatibility
        return $"BookingCare: Lich hen ngay {appointmentDate:dd/MM/yyyy} da bi huy. " +
               $"Hoan tra {refundAmount:N0} VND trong 5-7 ngay. Lien he: 1900xxxx";
    }

    /// <summary>
    /// Build SMS content for appointment cancellation refund (patient has NO bank account)
    /// </summary>
    public static string BuildRefundSmsNoBankAccount(
        DateTime appointmentDate,
        decimal refundAmount)
    {
        // Keep message short and include call-to-action
        return $"BookingCare: Lich hen ngay {appointmentDate:dd/MM/yyyy} da bi huy. " +
               $"Vui long cap nhat tai khoan ngan hang de nhan {refundAmount:N0} VND. " +
               $"Dang nhap: bookingcare.vn";
    }

    /// <summary>
    /// Build SMS content for OTP verification
    /// </summary>
    public static string BuildOtpSms(string otpCode, string purpose)
    {
        var safePurpose = string.IsNullOrWhiteSpace(purpose) ? "xac thuc" : purpose;
        return $"BookingCare: Ma OTP cua ban la {otpCode}. Ma co hieu luc trong 5 phut cho muc dich {safePurpose}.";
    }

    /// <summary>
    /// Build SMS content for password reset
    /// </summary>
    public static string BuildPasswordResetSms(string otpCode)
    {
        return $"BookingCare: Ma xac thuc dat lai mat khau: {otpCode}. Ma co hieu luc trong 5 phut. Khong chia se ma nay.";
    }

    /// <summary>
    /// Build generic notification SMS
    /// </summary>
    public static string BuildGenericSms(string message)
    {
        // Add BookingCare prefix if not present
        if (!message.StartsWith("BookingCare:", StringComparison.OrdinalIgnoreCase))
        {
            return $"BookingCare: {message}";
        }
        return message;
    }

    /// <summary>
    /// Build SMS content for no refund case (0% refund due to late cancellation)
    /// </summary>
    public static string BuildNoRefundSms(DateTime appointmentDate)
    {
        var appointmentDateStr = appointmentDate.ToString("dd/MM/yyyy HH:mm");

        return $"BookingCare: Lich hen {appointmentDateStr} da huy. Do huy qua sat gio, khong duoc hoan tien theo chinh sach. Xin loi vi bat tien.";
    }

    /// <summary>
    /// Build SMS content for staff-initiated cancellation with reschedule options
    /// Patient needs to check email for full details and choose from 4 options
    /// </summary>
    public static string BuildCancellationWithOptionsSms(DateTime appointmentDate, string? optionUrl)
    {
        var appointmentDateStr = appointmentDate.ToString("dd/MM/yyyy HH:mm");
        var urlPart = !string.IsNullOrEmpty(optionUrl) ? $" Chon phuong an: {optionUrl}" : "";

        return $"BookingCare: Lich hen {appointmentDateStr} da huy boi benh vien. Vui long kiem tra email de chon phuong an xu ly (doi lich hoac hoan tien).{urlPart}";
    }

    /// <summary>
    /// Build SMS content for doctor change refund (patient has bank account)
    /// </summary>
    public static string BuildDoctorChangeRefundSms(
        string originalDoctorName,
        string newDoctorName,
        decimal refundAmount)
    {
        return $"BookingCare: Doi bac si thanh cong tu {originalDoctorName} sang {newDoctorName}. " +
               $"Hoan tra chenh lech {refundAmount:N0} VND trong 5-7 ngay.";
    }

    /// <summary>
    /// Build SMS content for doctor change refund (patient has NO bank account)
    /// </summary>
    public static string BuildDoctorChangeRefundSmsNoBankAccount(
        string originalDoctorName,
        string newDoctorName,
        decimal refundAmount)
    {
        return $"BookingCare: Doi bac si thanh cong tu {originalDoctorName} sang {newDoctorName}. " +
               $"Cap nhat tai khoan ngan hang de nhan {refundAmount:N0} VND. " +
               $"Dang nhap: bookingcare.vn";
    }

    /// <summary>
    /// Build SMS content for rejection notification
    /// </summary>
    public static string BuildSmsContent(
        AppointmentRejectedNotificationEvent @event,
        string appointmentTime)
    {
        var hospitalInfo = !string.IsNullOrEmpty(@event.HospitalName)
            ? $" tại {TruncateText(@event.HospitalName, 20)}"
            : "";

        return $"[BookingCare] Yeu cau dat lich ngay {@event.AppointmentDate:dd/MM/yyyy} {appointmentTime}{hospitalInfo} da bi tu choi. Ly do: {TruncateText(@event.RejectionReason, 50)}. Vui long dat lich moi.";
    }

    /// <summary>
    /// Truncate text to specified length with ellipsis
    /// </summary>
    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
        {
            return text;
        }

        return text[..(maxLength - 3)] + "...";
    }
}

