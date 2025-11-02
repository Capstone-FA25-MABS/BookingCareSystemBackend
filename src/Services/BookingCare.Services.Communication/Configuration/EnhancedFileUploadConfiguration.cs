using BookingCare.Services.Communication.Configuration;

namespace BookingCare.Services.Communication.Configuration;

/// <summary>
/// Enhanced FileUpload Configuration v?i Hybrid Provider Support
/// Supports both Cloudinary and AWS S3 + CloudFront
/// </summary>
public class EnhancedFileUploadConfiguration
{
    public const string SectionName = "EnhancedFileUpload";

    /// <summary>
    /// Primary storage strategy: Auto, AWS, Cloudinary, Hybrid
    /// </summary>
    public string Strategy { get; set; } = "Hybrid";

    /// <summary>
    /// Routing rules for different file types
    /// </summary>
    public Dictionary<string, string> RoutingRules { get; set; } = new()
    {
        ["File"] = "AWS",           // Documents to AWS S3
        ["Audio"] = "AWS",          // Audio files to AWS S3  
        ["VoiceNote"] = "AWS",      // Voice notes to AWS S3
        ["Image"] = "Cloudinary",   // Images to Cloudinary for processing
        ["Video"] = "Cloudinary"    // Videos to Cloudinary for processing
    };

    /// <summary>
    /// AWS S3 Configuration
    /// </summary>
    public S3Config S3 { get; set; } = new();

    /// <summary>
    /// CloudFront Configuration
    /// </summary>
    public CloudFrontConfig CloudFront { get; set; } = new();

    /// <summary>
    /// Cloudinary Configuration (existing) - simplified reference
    /// </summary>
    public CloudinaryConfig? Cloudinary { get; set; }

    /// <summary>
    /// Performance and optimization settings
    /// </summary>
    public PerformanceConfig Performance { get; set; } = new();
}

/// <summary>
/// S3-specific configuration
/// </summary>
public class S3Config
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = "bookingcare-communication";
    public string Region { get; set; } = "ap-southeast-1";
    public int PresignedUrlExpiryHours { get; set; } = 1;
    public long MaxFileSizeBytes { get; set; } = 200 * 1024 * 1024; // 200MB
    public string[] AllowedFileExtensions { get; set; } =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".mp4", ".avi", ".mov", ".webm",
        ".mp3", ".wav", ".ogg", ".m4a",
        ".pdf", ".doc", ".docx", ".txt", ".rtf"
    };
    public string FileUploadPath { get; set; } = "communication/";
}

/// <summary>
/// CloudFront-specific configuration
/// </summary>
public class CloudFrontConfig
{
    public string DistributionId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int CacheTtlSeconds { get; set; } = 86400; // 1 day
    public bool EnableInvalidation { get; set; } = true;
    public string[] CacheableExtensions { get; set; } =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp",
        ".mp4", ".avi", ".mov", ".webm",
        ".mp3", ".wav", ".ogg", ".m4a",
        ".pdf"
    };
}

/// <summary>
/// Simple Cloudinary configuration
/// </summary>
public class CloudinaryConfig
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://res.cloudinary.com";
    public bool AutoOptimize { get; set; } = true;
    public bool AutoFormat { get; set; } = true;
}

/// <summary>
/// Performance optimization settings
/// </summary>
public class PerformanceConfig
{
    /// <summary>
    /// Enable parallel uploads for multiple files
    /// </summary>
    public bool EnableParallelUploads { get; set; } = true;

    /// <summary>
    /// Maximum concurrent uploads
    /// </summary>
    public int MaxConcurrentUploads { get; set; } = 3;

    /// <summary>
    /// Enable automatic compression for images
    /// </summary>
    public bool EnableImageCompression { get; set; } = true;

    /// <summary>
    /// Enable automatic format conversion (WebP, etc.)
    /// </summary>
    public bool EnableFormatOptimization { get; set; } = true;

    /// <summary>
    /// Thumbnail generation settings
    /// </summary>
    public ThumbnailConfig Thumbnails { get; set; } = new();
}

/// <summary>
/// Thumbnail generation configuration
/// </summary>
public class ThumbnailConfig
{
    public bool EnableAutoGeneration { get; set; } = true;
    public int DefaultSize { get; set; } = 300;
    public string Quality { get; set; } = "auto:good";
    public string[] SupportedFormats { get; set; } = { "jpg", "png", "webp" };
}