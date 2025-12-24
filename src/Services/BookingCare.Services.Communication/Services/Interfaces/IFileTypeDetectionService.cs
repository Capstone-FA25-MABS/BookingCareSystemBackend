using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Service to automatically detect file types and map to appropriate storage strategy
/// </summary>
public interface IFileTypeDetectionService
{
    /// <summary>
    /// Detect detailed message type from file content and metadata
    /// </summary>
    Task<DetailedMessageType> DetectMessageTypeAsync(IFormFile file);

    /// <summary>
    /// Get appropriate storage folder based on detected type
    /// </summary>
    string GetStorageFolder(string userId, DetailedMessageType detectedType);

    /// <summary>
    /// Determine if file should use Cloudinary or S3 based on detected type
    /// </summary>
    bool ShouldUseCloudinary(DetailedMessageType detectedType, string contentType);

    /// <summary>
    /// Get file category for UI grouping
    /// </summary>
    string GetFileCategory(DetailedMessageType detectedType);

    /// <summary>
    /// Validate if file is allowed based on detected type
    /// </summary>
    Task<FileDetectionResult> ValidateAndDetectAsync(IFormFile file);
}

/// <summary>
/// Result of file type detection and validation
/// </summary>
public class FileDetectionResult
{
    public bool IsValid { get; set; }
    public DetailedMessageType DetectedType { get; set; }
    public string StorageFolder { get; set; } = string.Empty;
    public bool UseCloudinary { get; set; }
    public string FileCategory { get; set; } = string.Empty;
    public string DetectedMimeType { get; set; } = string.Empty;
    public List<string> ValidationErrors { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}