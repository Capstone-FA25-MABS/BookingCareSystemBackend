namespace BookingCare.Services.Payment.Models.Configurations;

/// <summary>
/// VNPay configuration
/// </summary>
public class VNPayConfiguration
{
    /// <summary>
    /// VNPay payment URL
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// VNPay return URL for callbacks
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// TMN Code (Terminal Code) provided by VNPay
    /// </summary>
    public string TmnCode { get; set; } = string.Empty;

    /// <summary>
    /// Hash secret provided by VNPay
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>
    /// VNPay API version
    /// </summary>
    public string Version { get; set; } = "2.1.0";

    /// <summary>
    /// Payment command
    /// </summary>
    public string Command { get; set; } = "pay";

    /// <summary>
    /// Currency code (e.g. VND)
    /// </summary>
    public string CurrCode { get; set; } = "VND";

    /// <summary>
    /// Locale (vi or en)
    /// </summary>
    public string Locale { get; set; } = "vn";

    /// <summary>
    /// Transaction timeout in minutes
    /// </summary>
    public int TimeoutInMinutes { get; set; } = 15;
}