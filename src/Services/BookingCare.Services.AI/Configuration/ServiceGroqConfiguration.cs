namespace BookingCare.Services.AI.Configuration;

/// <summary>
/// Cấu hình Groq API cho từng service riêng biệt
/// Configuration for individual service Groq API settings
/// </summary>
public class ServiceGroqConfiguration
{
    /// <summary>
    /// API Key riêng cho service này
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model chính sử dụng (mặc định: qwen/qwen3-32b)
    /// </summary>
    public string PrimaryModel { get; set; } = "qwen/qwen3-32b";

    /// <summary>
    /// Model dự phòng khi model chính thất bại (mặc định: qwen/qwen3-32b)
    /// </summary>
    public string FallbackModel { get; set; } = "qwen/qwen3-32b";

    /// <summary>
    /// Max output tokens. Nếu null, dùng common config.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Temperature. Nếu null, dùng common config.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Maximum retries for API calls. Nếu null, dùng common config.
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Timeout in seconds. Nếu null, dùng common config.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

