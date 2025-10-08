namespace BookingCare.Services.Payment.Models.Configurations;

/// <summary>
/// PayOS configuration
/// </summary>
public class PayOSConfiguration
{
    /// <summary>
    /// Client ID from PayOS Dashboard
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// API Key from PayOS Dashboard
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Checksum Key from PayOS Dashboard
    /// </summary>
    public string ChecksumKey { get; set; } = string.Empty;

    /// <summary>
    /// Callback URL to receive payment results
    /// </summary>
    public string ReturnUrl { get; set; } = string.Empty;

    /// <summary>
    /// Cancel URL to receive result when user cancels payment
    /// </summary>
    public string CancelUrl { get; set; } = string.Empty;

    /// <summary>
    /// Environment (sandbox or production)
    /// </summary>
    public string Environment { get; set; } = "sandbox";

    /// <summary>
    /// Transaction timeout in minutes
    /// </summary>
    public int TimeoutInMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to use webhook
    /// </summary>
    public bool UseWebhook { get; set; } = true;

    /// <summary>
    /// Webhook URL to receive notifications from PayOS
    /// </summary>
    public string WebhookUrl { get; set; } = string.Empty;
}