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
}

