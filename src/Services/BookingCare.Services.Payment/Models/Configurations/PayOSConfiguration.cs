namespace BookingCare.Services.Payment.Models.Configurations;

/// <summary>
/// C?u hình PayOS
/// </summary>
public class PayOSConfiguration
{
    /// <summary>
    /// Client ID t? PayOS Dashboard
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// API Key t? PayOS Dashboard
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Checksum Key t? PayOS Dashboard
    /// </summary>
    public string ChecksumKey { get; set; } = string.Empty;

    /// <summary>
    /// URL callback ?? nh?n k?t qu? thanh toán
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// URL cancel ?? nh?n k?t qu? khi user h?y thanh toán
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Môi tr??ng (sandbox ho?c production)
    /// </summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Th?i gian timeout cho giao d?ch (phút)
    /// </summary>
    public int TimeoutInMinutes { get; set; } = 15;

    /// <summary>
    /// Có s? d?ng webhook không
    /// </summary>
    public bool UseWebhook { get; set; } = true;

    /// <summary>
    /// URL webhook ?? nh?n thông báo t? PayOS
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;
}