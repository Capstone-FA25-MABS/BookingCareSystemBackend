namespace BookingCare.Services.Communication.Configuration;

/// <summary>
/// Configuration cho file upload constraints
/// </summary>
public class FileUploadConfiguration
{
    public const string SectionName = "FileUpload";

    /// <summary>
    /// Cloud storage provider (Azure, AWS, GoogleCloud, Cloudinary, Local)
    /// </summary>
    public string Provider { get; set; } = "Cloudinary";

    /// <summary>
    /// Base URL cho CDN
    /// </summary>
    public string CdnBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Container/bucket name
    /// </summary>
    public string ContainerName { get; set; } = "communication-files";

    /// <summary>
    /// Constraints cho t?ng lo?i message
    /// </summary>
    public Dictionary<string, MessageTypeConstraints> Constraints { get; set; } = new()
    {
        ["Image"] = new MessageTypeConstraints
        {
            MaxSizeBytes = 10 * 1024 * 1024, // 10MB
            AllowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" },
            MaxWidth = 4096,
            MaxHeight = 4096,
            GenerateThumbnail = true,
            ThumbnailSize = 300
        },
        ["Video"] = new MessageTypeConstraints
        {
            MaxSizeBytes = 100 * 1024 * 1024, // 100MB
            AllowedMimeTypes = new[] { "video/mp4", "video/avi", "video/mov", "video/webm" },
            MaxDurationSeconds = 300, // 5 minutes
            GenerateThumbnail = true,
            ThumbnailSize = 300,
            GenerateVariants = true
        },
        ["Audio"] = new MessageTypeConstraints
        {
            MaxSizeBytes = 50 * 1024 * 1024, // 50MB
            AllowedMimeTypes = new[] { "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a" },
            MaxDurationSeconds = 600, // 10 minutes
            GenerateWaveform = true
        },
        ["VoiceNote"] = new MessageTypeConstraints
        {
            MaxSizeBytes = 10 * 1024 * 1024, // 10MB
            AllowedMimeTypes = new[] { "audio/mp3", "audio/wav", "audio/ogg", "audio/m4a" },
            MaxDurationSeconds = 120, // 2 minutes
            GenerateWaveform = true
        },
        ["File"] = new MessageTypeConstraints
        {
            MaxSizeBytes = 200 * 1024 * 1024, // 200MB
            AllowedMimeTypes = new[] 
            { 
                "application/pdf", 
                "application/msword", 
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/zip",
                "application/x-rar-compressed"
            }
        }
    };

    /// <summary>
    /// Cloudinary settings
    /// </summary>
    public CloudinarySettings? Cloudinary { get; set; }

    /// <summary>
    /// Azure Blob Storage settings
    /// </summary>
    public AzureBlobSettings? Azure { get; set; }

    /// <summary>
    /// AWS S3 settings
    /// </summary>
    public AwsS3Settings? Aws { get; set; }

    /// <summary>
    /// Google Cloud Storage settings
    /// </summary>
    public GoogleCloudSettings? GoogleCloud { get; set; }
}

/// <summary>
/// Cloudinary configuration
/// </summary>
public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool AutoOptimize { get; set; } = true;
    public bool AutoFormat { get; set; } = true;
}

/// <summary>
/// Constraints cho t?ng lo?i message type
/// </summary>
public class MessageTypeConstraints
{
    public long MaxSizeBytes { get; set; }
    public string[] AllowedMimeTypes { get; set; } = Array.Empty<string>();
    public int? MaxWidth { get; set; }
    public int? MaxHeight { get; set; }
    public int? MaxDurationSeconds { get; set; }
    public bool GenerateThumbnail { get; set; } = false;
    public int ThumbnailSize { get; set; } = 300;
    public bool GenerateVariants { get; set; } = false;
    public bool GenerateWaveform { get; set; } = false;
}

/// <summary>
/// Azure Blob Storage configuration
/// </summary>
public class AzureBlobSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string CdnEndpoint { get; set; } = string.Empty;
}

/// <summary>
/// AWS S3 configuration
/// </summary>
public class AwsS3Settings
{
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string CloudFrontDomain { get; set; } = string.Empty;
}

/// <summary>
/// Google Cloud Storage configuration
/// </summary>
public class GoogleCloudSettings
{
    public string ProjectId { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
    public string ServiceAccountKeyPath { get; set; } = string.Empty;
    public string CdnDomain { get; set; } = string.Empty;
}