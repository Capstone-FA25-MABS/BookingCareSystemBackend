namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Cấu hình Gemini API cho từng service riêng biệt
/// Configuration for individual service Gemini API settings
/// </summary>
public class ServiceGeminiConfiguration
{
    /// <summary>
    /// API Key riêng cho service này
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model chính sử dụng (mặc định: gemini-2.5-pro)
    /// </summary>
    public string PrimaryModel { get; set; } = "gemini-2.5-pro";

    /// <summary>
    /// Model dự phòng khi model chính thất bại (mặc định: gemini-2.5-flash)
    /// </summary>
    public string FallbackModel { get; set; } = "gemini-2.5-flash";

    /// <summary>
    /// Max output tokens. Nếu null, dùng common config.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Temperature. Nếu null, dùng common config.
    /// </summary>
    public double? Temperature { get; set; }
}
