using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Configuration;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Smart file type detection service with auto-categorization
/// </summary>
public class FileTypeDetectionService : IFileTypeDetectionService
{

    private readonly ILogger<FileTypeDetectionService> _logger;

    // File signature patterns for accurate detection
    private static readonly Dictionary<string, byte[]> FileSignatures = new()
    {
        // Images
        ["image/jpeg"] = new byte[] { 0xFF, 0xD8 },
        ["image/png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
        ["image/gif"] = new byte[] { 0x47, 0x49, 0x46 },
        ["image/webp"] = new byte[] { 0x52, 0x49, 0x46, 0x46 },

        // Videos
        ["video/mp4"] = new byte[] { 0x66, 0x74, 0x79, 0x70 }, // ftyp
        ["video/avi"] = new byte[] { 0x52, 0x49, 0x46, 0x46 }, // RIFF

        // Audio
        ["audio/mp3"] = new byte[] { 0xFF, 0xFB }, // MP3 frame header
        ["audio/wav"] = new byte[] { 0x52, 0x49, 0x46, 0x46 }, // RIFF

        // Documents
        ["application/pdf"] = new byte[] { 0x25, 0x50, 0x44, 0x46 }, // %PDF
        ["application/zip"] = new byte[] { 0x50, 0x4B, 0x03, 0x04 }, // PK
    };

    public FileTypeDetectionService(

        ILogger<FileTypeDetectionService> logger)
    {

        _logger = logger;
    }

    /// <summary>
    /// Intelligent file type detection using multiple strategies
    /// </summary>
    public async Task<DetailedMessageType> DetectMessageTypeAsync(IFormFile file)
    {
        try
        {
            var fileName = file.FileName.ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();
            var extension = Path.GetExtension(fileName);

            _logger.LogDebug("Detecting file type for {FileName} with content type {ContentType}",
                fileName, contentType);

            // Strategy 1: File signature detection (most reliable)
            var signatureType = await DetectByFileSignatureAsync(file);
            if (signatureType != DetailedMessageType.Other)
            {
                _logger.LogDebug("File type detected by signature: {Type}", signatureType);
                return signatureType;
            }

            // Strategy 2: Content-Type header detection
            var contentTypeResult = DetectByContentType(contentType);
            if (contentTypeResult != DetailedMessageType.Other)
            {
                _logger.LogDebug("File type detected by content type: {Type}", contentTypeResult);
                return contentTypeResult;
            }

            // Strategy 3: File extension detection (fallback)
            var extensionResult = DetectByExtension(extension);
            _logger.LogDebug("File type detected by extension: {Type}", extensionResult);
            return extensionResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting file type for {FileName}", file.FileName);
            return DetailedMessageType.Other;
        }
    }

    /// <summary>
    /// Generate appropriate storage folder based on detected type
    /// </summary>
    public string GetStorageFolder(string userId, DetailedMessageType detectedType)
    {
        var typeFolder = detectedType switch
        {
            DetailedMessageType.Image => "images",
            DetailedMessageType.Video => "videos",
            DetailedMessageType.Audio => "audio",
            DetailedMessageType.VoiceNote => "voicenotes",
            DetailedMessageType.Document => "documents",
            DetailedMessageType.Gif => "gifs",
            DetailedMessageType.Archive => "archives",
            _ => "files"
        };

        return $"communication/{userId}/{typeFolder}";
    }

    /// <summary>
    /// Smart routing decision: Cloudinary for rich media, S3 for everything else
    /// </summary>
    public bool ShouldUseCloudinary(DetailedMessageType detectedType, string contentType)
    {
        return detectedType switch
        {
            DetailedMessageType.Image => true,    // Rich image processing
            DetailedMessageType.Video => true,    // Video processing & thumbnails
            DetailedMessageType.Gif => true,      // Animation processing
            _ => false                             // Everything else goes to S3
        };
    }

    /// <summary>
    /// Get user-friendly category for UI grouping
    /// </summary>
    public string GetFileCategory(DetailedMessageType detectedType)
    {
        return detectedType switch
        {
            DetailedMessageType.Image or DetailedMessageType.Gif => "Media",
            DetailedMessageType.Video => "Media",
            DetailedMessageType.Audio or DetailedMessageType.VoiceNote => "Audio",
            DetailedMessageType.Document => "Documents",
            DetailedMessageType.Archive => "Archives",
            _ => "Files"
        };
    }

    /// <summary>
    /// Comprehensive validation and detection in one step
    /// </summary>
    public async Task<FileDetectionResult> ValidateAndDetectAsync(IFormFile file)
    {
        var result = new FileDetectionResult();
        var fileName = file?.FileName ?? "unknown";

        try
        {
            // Basic validation
            if (file == null || file.Length == 0)
            {
                result.ValidationErrors.Add("File is required and cannot be empty");
                return result;
            }

            // Detect file type
            var detectedType = await DetectMessageTypeAsync(file);
            var detectedMimeType = await GetActualMimeTypeAsync(file);

            result.DetectedType = detectedType;
            result.DetectedMimeType = detectedMimeType;
            result.StorageFolder = GetStorageFolder("", detectedType); // userId will be set later
            result.UseCloudinary = ShouldUseCloudinary(detectedType, detectedMimeType);
            result.FileCategory = GetFileCategory(detectedType);

            // Validate file size based on detected type
            var maxSize = GetMaxSizeForType(detectedType);
            if (file.Length > maxSize)
            {
                result.ValidationErrors.Add(
                    $"File size ({file.Length:N0} bytes) exceeds maximum allowed for {detectedType} ({maxSize:N0} bytes)");
            }

            // Validate allowed types
            if (!IsTypeAllowed(detectedType))
            {
                result.ValidationErrors.Add($"File type '{detectedType}' is not allowed");
            }

            // Add metadata
            result.Metadata = new Dictionary<string, object>
            {
                ["OriginalFileName"] = file.FileName,
                ["ContentType"] = file.ContentType,
                ["DetectedMimeType"] = detectedMimeType,
                ["FileSize"] = file.Length,
                ["DetectionStrategy"] = GetDetectionStrategy(file, detectedType),
                ["RecommendedStorage"] = result.UseCloudinary ? "Cloudinary" : "AWS S3"
            };

            result.IsValid = !result.ValidationErrors.Any();

            _logger.LogInformation("File detection completed: {FileName} -> {DetectedType} (Valid: {IsValid})",
                fileName, detectedType, result.IsValid);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in file validation and detection for {FileName}", fileName);
            result.ValidationErrors.Add($"Detection error: {ex.Message}");
            return result;
        }
    }

    #region Private Helper Methods

    private static async Task<DetailedMessageType> DetectByFileSignatureAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[8];
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            stream.Position = 0;

            // Only check signatures if we have enough bytes
            if (bytesRead < 2)
            {
                return DetailedMessageType.Other;
            }

            foreach (var signature in FileSignatures)
            {
                // Check if we read enough bytes for this signature
                if (bytesRead >= signature.Value.Length &&
                    buffer.Take(signature.Value.Length).SequenceEqual(signature.Value))
                {
                    return MapMimeTypeToDetailedType(signature.Key);
                }
            }

            return DetailedMessageType.Other;
        }
        catch
        {
            return DetailedMessageType.Other;
        }
    }

    private static DetailedMessageType DetectByContentType(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return DetailedMessageType.Other;

        return contentType switch
        {
            var ct when ct.StartsWith("image/gif") => DetailedMessageType.Gif,
            var ct when ct.StartsWith("image/") => DetailedMessageType.Image,
            var ct when ct.StartsWith("video/") => DetailedMessageType.Video,
            var ct when ct.StartsWith("audio/") => DetailedMessageType.Audio,
            "application/pdf" => DetailedMessageType.Document,
            var ct when ct.Contains("word") || ct.Contains("excel") || ct.Contains("powerpoint") => DetailedMessageType.Document,
            var ct when ct.Contains("zip") || ct.Contains("rar") || ct.Contains("7z") => DetailedMessageType.Archive,
            "text/plain" => DetailedMessageType.Document,
            _ => DetailedMessageType.Other
        };
    }

    private static DetailedMessageType DetectByExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" => DetailedMessageType.Image,
            ".gif" => DetailedMessageType.Gif,
            ".mp4" or ".avi" or ".mov" or ".webm" or ".mkv" => DetailedMessageType.Video,
            ".mp3" or ".wav" or ".ogg" or ".m4a" or ".aac" => DetailedMessageType.Audio,
            ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" => DetailedMessageType.Document,
            ".xls" or ".xlsx" or ".csv" => DetailedMessageType.Document,
            ".ppt" or ".pptx" => DetailedMessageType.Document,
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => DetailedMessageType.Archive,
            _ => DetailedMessageType.Other
        };
    }

    private static DetailedMessageType MapMimeTypeToDetailedType(string mimeType)
    {
        return mimeType switch
        {
            var mt when mt.StartsWith("image/gif") => DetailedMessageType.Gif,
            var mt when mt.StartsWith("image/") => DetailedMessageType.Image,
            var mt when mt.StartsWith("video/") => DetailedMessageType.Video,
            var mt when mt.StartsWith("audio/") => DetailedMessageType.Audio,
            "application/pdf" => DetailedMessageType.Document,
            _ => DetailedMessageType.Other
        };
    }

    private async static Task<string> GetActualMimeTypeAsync(IFormFile file)
    {
        // Could implement more sophisticated MIME type detection here
        // For now, return the provided content type
        await Task.CompletedTask;
        return file.ContentType;
    }

    private static long GetMaxSizeForType(DetailedMessageType type)
    {
        return type switch
        {
            DetailedMessageType.Image => 10 * 1024 * 1024,      // 10MB
            DetailedMessageType.Video => 100 * 1024 * 1024,     // 100MB  
            DetailedMessageType.Audio => 50 * 1024 * 1024,      // 50MB
            DetailedMessageType.VoiceNote => 10 * 1024 * 1024,  // 10MB
            DetailedMessageType.Document => 200 * 1024 * 1024,  // 200MB
            DetailedMessageType.Archive => 500 * 1024 * 1024,   // 500MB
            _ => 200 * 1024 * 1024                               // 200MB default
        };
    }

    private static bool IsTypeAllowed(DetailedMessageType type)
    {
        // All detected types are allowed for now
        // Could add configuration for restricted types
        return type != DetailedMessageType.Other;
    }

    private static string GetDetectionStrategy(IFormFile file, DetailedMessageType detectedType)
    {
        // Simple heuristic to understand how type was detected
        var contentType = file.ContentType.ToLowerInvariant();


        if (FileSignatures.Any(sig => contentType.Contains(sig.Key)))
            return "FileSignature";

        if (DetectByContentType(contentType) == detectedType)
            return "ContentType";

        return "FileExtension";
    }

    #endregion
}