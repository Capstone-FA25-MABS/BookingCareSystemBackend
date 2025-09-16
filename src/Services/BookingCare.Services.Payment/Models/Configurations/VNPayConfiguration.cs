namespace BookingCare.Services.Payment.Models.Configurations;

/// <summary>
/// Cấu hình VNPay
/// </summary>
public class VNPayConfiguration
{
    /// <summary>
    /// URL thanh toán của VNPay
    /// </summary>
    public string PaymentUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL trả kết quả từ VNPay về
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// TMN Code (Terminal Code) được VNPay cấp
    /// </summary>
    public string TmnCode { get; set; } = string.Empty;

    /// <summary>
    /// Hash Secret được VNPay cấp
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>
    /// API Version của VNPay
    /// </summary>
    public string Version { get; set; } = "2.1.0";

    /// <summary>
    /// Command giao dịch thanh toán
    /// </summary>
    public string Command { get; set; } = "pay";

    /// <summary>
    /// Currency Code (VND)
    /// </summary>
    public string CurrCode { get; set; } = "VND";

    /// <summary>
    /// Locale (vi hoặc en)
    /// </summary>
    public string Locale { get; set; } = "vn";

    /// <summary>
    /// Thời gian timeout cho giao dịch (phút)
    /// </summary>
    public int TimeoutInMinutes { get; set; } = 15;
}