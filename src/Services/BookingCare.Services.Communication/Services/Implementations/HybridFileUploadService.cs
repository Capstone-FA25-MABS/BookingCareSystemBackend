using AutoMapper;
using BookingCare.Services.Communication.Configuration;
using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Shared.FileUpload.Models;
using Microsoft.Extensions.Options;
// Type aliases to resolve ambiguous references
using CommFileUploadResult = BookingCare.Services.Communication.Services.Interfaces.FileUploadResult;
using CommPresignedUrlResult = BookingCare.Services.Communication.Services.Interfaces.PresignedUrlResult;
using S3FileUploadService = BookingCare.Shared.FileUpload.Services.IFileUploadService;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// S3-Only File Upload Service (formerly Hybrid)
/// Routes all files to AWS S3 + CloudFront with smart detection and organization
/// Maintains backward compatibility with existing Communication Service APIs
/// </summary>
public class HybridFileUploadService : IHybridFileUploadService
{
    private readonly S3FileUploadService _s3Service; // AWS S3 + CloudFront
    private readonly FileUploadConfiguration _config;
    private readonly ILogger<HybridFileUploadService> _logger;

    public HybridFileUploadService(
        S3FileUploadService s3Service,
        IOptions<FileUploadConfiguration> config,
        ILogger<HybridFileUploadService> logger
    )
    {
        _s3Service = s3Service;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Smart routing: All files go to S3 with intelligent organization
    /// Enhanced with automatic file type detection and correction
    /// </summary>
    public async Task<CommFileUploadResult> UploadFileAsync(
        IFormFile file,
        string userId,
        MessageType messageType
    )
    {
        try
        {
            _logger.LogInformation(
                "Starting S3-only file upload for user {UserId}, file {FileName}, client type {MessageType}",
                userId,
                file.FileName,
                messageType
            );

            // ?? SMART ENHANCEMENT: Auto-detect actual file type
            var detectedType = await AutoDetectFileTypeAsync(file);
            var smartMessageType = MapDetectedToMessageType(detectedType);

            // Determine final message type with smart override logic
            var finalMessageType = DetermineOptimalMessageType(messageType, smartMessageType, file);

            _logger.LogInformation(
                "Smart file type analysis: Client={ClientType}, Detected={DetectedType}, Final={FinalType}, File={FileName}",
                messageType, detectedType, finalMessageType, file.FileName
            );

            // All files go to S3 in S3-only mode
            _logger.LogInformation(
                "S3-only mode: routing to AWS S3 for {FinalType} with content type {ContentType}",
                finalMessageType,
                file.ContentType
            );
            return await UploadToS3Async(file, userId, finalMessageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error in S3-only file upload for user {UserId}, file {FileName}",
                userId,
                file.FileName
            );
            throw;
        }
    }

    /// <summary>
    /// Upload multiple files with smart routing
    /// </summary>
    public async Task<List<CommFileUploadResult>> UploadMultipleFilesAsync(
        IEnumerable<IFormFile> files,
        string userId,
        MessageType messageType
    )
    {
        var tasks = files.Select(file => UploadFileAsync(file, userId, messageType));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Generate presigned URL with provider routing
    /// </summary>
    public async Task<CommPresignedUrlResult> GeneratePresignedUrlAsync(
        string fileName,
        string contentType,
        string userId,
        MessageType messageType
    )
    {
        var request = new PresignedUrlRequest
        {
            FileName = fileName,
            ContentType = contentType,
            Folder = GenerateS3Folder(userId, messageType),
            ExpiryHours = 1,
        };

        var s3Result = await _s3Service.GeneratePresignedUploadUrlAsync(request);

        // Map to Communication Service format
        return new CommPresignedUrlResult
        {
            UploadUrl = s3Result.UploadUrl,
            FinalUrl = s3Result.CloudFrontUrl,
            ExpiresAt = s3Result.ExpiresAt,
            Headers = new Dictionary<string, string>(),
        };
    }

    /// <summary>
    /// Delete file from S3 storage
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        if (IsS3Url(fileUrl))
        {
            var s3Key = ExtractS3KeyFromUrl(fileUrl);
            return await _s3Service.DeleteFileAsync(s3Key);
        }
        else
        {
            _logger.LogWarning("Cannot delete non-S3 URL in S3-only mode: {FileUrl}", fileUrl);
            return false;
        }
    }

    /// <summary>
    /// Generate thumbnail (S3-compatible - simple implementation)
    /// </summary>
    public async Task<string?> GenerateThumbnailAsync(string originalUrl)
    {
        // In S3-only mode, we don't have auto thumbnail generation like Cloudinary
        // Return original URL or implement Lambda-based thumbnail generation
        _logger.LogInformation("S3-only mode: thumbnail generation not implemented, returning original URL");
        await Task.CompletedTask;
        return originalUrl; // Simple fallback - return original URL
    }

    /// <summary>
    /// Enhanced validation considering both providers
    /// </summary>
    public async Task<FileValidationResult> ValidateFileAsync(
        IFormFile file,
        MessageType messageType
    )
    {
        var errors = new List<string>();

        // Basic validation
        if (file == null || file.Length == 0)
        {
            errors.Add("File is required");
            return new FileValidationResult { IsValid = false, Errors = errors };
        }

        // Validate MessageType consistency with file content
        var contentTypeValidation = ValidateMessageTypeConsistency(messageType, file.ContentType, file.FileName);
        if (!contentTypeValidation.IsValid)
        {
            errors.AddRange(contentTypeValidation.Errors);
        }

        // Get constraints for MessageType
        if (!_config.Constraints.TryGetValue(messageType.ToString(), out var constraints))
        {
            errors.Add($"No constraints defined for message type: {messageType}");
            return new FileValidationResult { IsValid = false, Errors = errors };
        }

        // Size validation
        if (file.Length > constraints.MaxSizeBytes)
        {
            errors.Add(
                $"File size ({file.Length:N0} bytes) exceeds maximum allowed ({constraints.MaxSizeBytes:N0} bytes)"
            );
        }

        // MIME type validation
        if (
            !constraints.AllowedMimeTypes.Contains(
                file.ContentType,
                StringComparer.OrdinalIgnoreCase
            )
        )
        {
            errors.Add($"File type '{file.ContentType}' is not allowed for {messageType}");
        }

        // Additional validation for images/videos
        if (messageType == MessageType.Image || messageType == MessageType.Video)
        {
            await ValidateMediaFileAsync(file, constraints, errors);
        }

        return new FileValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            DetectedMimeType = file.ContentType,
        };
    }

    /// <summary>
    /// Upload directly to AWS S3
    /// </summary>
    public async Task<CommFileUploadResult> UploadToS3Async(
        IFormFile file,
        string userId,
        MessageType messageType,
        string? customFolder = null
    )
    {
        using var stream = file.OpenReadStream();

        var request = new FileUploadRequest
        {
            FileStream = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Folder = customFolder ?? GenerateS3Folder(userId, messageType),
            GenerateUniqueFileName = true,
            Metadata = new Dictionary<string, string>
            {
                ["UserId"] = userId,
                ["MessageType"] = messageType.ToString(),
                ["UploadedAt"] = DateTime.UtcNow.ToString("O"),
                ["ServiceName"] = "Communication",
            },
        };

        var s3Result = await _s3Service.UploadFileAsync(request);

        // Get file info for images/videos (basic info without processing)
        var (width, height, duration) = await GetBasicMediaInfoAsync(file);

        // Map to Communication Service format
        var result = new CommFileUploadResult
        {
            Url = s3Result.CloudFrontUrl, // Use CloudFront URL for better performance
            FileName = s3Result.FileName,
            Size = file.Length,
            MimeType = file.ContentType,
            ThumbnailUrl = null, // S3 doesn't auto-generate thumbnails by default
            Width = width,
            Height = height,
            Duration = duration,
        };

        // Generate simple thumbnail URL for images if needed
        if (ShouldGenerateS3Thumbnail(messageType, file.ContentType))
        {
            result.ThumbnailUrl = GenerateS3ThumbnailUrl(s3Result.CloudFrontUrl, messageType);
        }

        return result;
    }

    /// <summary>
    /// Get CloudFront URL for S3 files
    /// </summary>
    public string GetCloudFrontUrl(string s3Key)
    {
        return _s3Service.GetCloudFrontUrl(s3Key);
    }

    /// <summary>
    /// Check if file exists in S3 storage
    /// </summary>
    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        if (IsS3Url(fileUrl))
        {
            var s3Key = ExtractS3KeyFromUrl(fileUrl);
            return await _s3Service.FileExistsAsync(s3Key);
        }
        else
        {
            _logger.LogWarning("Cannot check existence of non-S3 URL in S3-only mode: {FileUrl}", fileUrl);
            return false;
        }
    }

    #region Private Helper Methods

    private bool IsS3Url(string url)
    {
        return url.Contains("cloudfront.net") || url.Contains("amazonaws.com");
    }

    private string ExtractS3KeyFromUrl(string url)
    {
        // Implementation to extract S3 key from CloudFront or S3 URL
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }

    private string GenerateS3Folder(string userId, MessageType messageType)
    {
        var folderName = GetSmartFolderName(messageType);
        return $"communication/{userId}/{folderName}";
    }

    /// <summary>
    /// Get appropriate folder name based on message type for better organization
    /// </summary>
    private string GetSmartFolderName(MessageType messageType)
    {
        return messageType switch
        {
            MessageType.Image => "images",
            MessageType.Video => "videos",
            MessageType.Audio => "audio",
            MessageType.VoiceNote => "voicenotes", 
            MessageType.Gif => "gifs",
            MessageType.File => "documents",
            MessageType.Text => "text",  // For text files
            _ => "files"  // Default fallback
        };
    }

    private bool ShouldGenerateThumbnail(MessageType messageType, string contentType)
    {
        if (messageType != MessageType.Image && messageType != MessageType.Video)
            return false;

        if (!_config.Constraints.TryGetValue(messageType.ToString(), out var constraints))
            return false;

        return constraints.GenerateThumbnail;
    }

    private bool ShouldGenerateS3Thumbnail(MessageType messageType, string contentType)
    {
        // In S3 mode, we only generate simple thumbnail URLs for images
        return messageType == MessageType.Image && contentType.StartsWith("image/");
    }

    private string? GenerateS3ThumbnailUrl(string originalUrl, MessageType messageType)
    {
        try
        {
            // For S3, we can't generate thumbnails on-the-fly like Cloudinary
            // Options:
            // 1. Return the original URL as thumbnail (simple approach)
            // 2. Use query parameter to indicate thumbnail request (if you have Lambda processing)
            // 3. Generate separate thumbnail file path
            
            // Simple approach: return original URL for now
            // You can enhance this with AWS Lambda for actual thumbnail generation
            if (messageType == MessageType.Image)
            {
                return originalUrl; // Use original image as thumbnail for now
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating S3 thumbnail URL for {Url}", originalUrl);
            return null;
        }
    }

    private async Task<(int? width, int? height, int? duration)> GetBasicMediaInfoAsync(IFormFile file)
    {
        try
        {
            // For S3-only mode, we don't do complex media analysis
            // Just return basic info if it's an image
            if (file.ContentType.StartsWith("image/"))
            {
                // Could add basic image dimension reading here if needed
                // For now, return null to keep it simple
                return (null, null, null);
            }
            
            return (null, null, null);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private async Task ValidateMediaFileAsync(
        IFormFile file,
        MessageTypeConstraints constraints,
        List<string> errors
    )
    {
        // Enhanced media validation could be added here
        await Task.CompletedTask;
    }

    /// <summary>
    /// Validate that MessageType is consistent with file content type
    /// </summary>
    private FileValidationResult ValidateMessageTypeConsistency(MessageType messageType, string contentType, string fileName)
    {
        var errors = new List<string>();
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        bool isValid = true;

        switch (messageType)
        {
            case MessageType.Image:
                if (!contentType.StartsWith("image/"))
                {
                    isValid = false; // HARD REJECT - không ph?i warning
                    errors.Add($"? REJECTED: MessageType is 'Image' but file content type is '{contentType}'. Expected image/* content type.");
                    errors.Add($"?? SOLUTION: Use MessageType.File for document files like '{fileName}'");
                    errors.Add($"?? FILE INFO: Extension '{fileExtension}', ContentType '{contentType}'");
                }
                break;

            case MessageType.Video:
                if (!contentType.StartsWith("video/"))
                {
                    isValid = false; // HARD REJECT
                    errors.Add($"? REJECTED: MessageType is 'Video' but file content type is '{contentType}'. Expected video/* content type.");
                    errors.Add($"?? SOLUTION: Use MessageType.File for document files like '{fileName}'");
                    errors.Add($"?? FILE INFO: Extension '{fileExtension}', ContentType '{contentType}'");
                }
                break;

            case MessageType.Audio:
            case MessageType.VoiceNote:
                if (!contentType.StartsWith("audio/"))
                {
                    isValid = false; // HARD REJECT
                    errors.Add($"? REJECTED: MessageType is '{messageType}' but file content type is '{contentType}'. Expected audio/* content type.");
                    errors.Add($"?? SOLUTION: Use MessageType.File for document files like '{fileName}'");
                    errors.Add($"?? FILE INFO: Extension '{fileExtension}', ContentType '{contentType}'");
                }
                break;

            case MessageType.File:
                // File type is flexible, can accept any content type
                // But provide helpful suggestions (INFO only - không reject)
                if (contentType.StartsWith("image/"))
                {
                    errors.Add($"?? INFO: File '{fileName}' appears to be an image. Consider using MessageType.Image for better optimization.");
                }
                else if (contentType.StartsWith("video/"))
                {
                    errors.Add($"?? INFO: File '{fileName}' appears to be a video. Consider using MessageType.Video for better processing.");
                }
                else if (contentType.StartsWith("audio/"))
                {
                    errors.Add($"?? INFO: File '{fileName}' appears to be audio. Consider using MessageType.Audio ho?c MessageType.VoiceNote.");
                }
                break;

            case MessageType.Text:
                if (contentType != "text/plain" && !fileExtension.Equals(".txt"))
                {
                    isValid = false; // HARD REJECT
                    errors.Add($"? REJECTED: MessageType is 'Text' but file is not a text file. Content type: '{contentType}'");
                    errors.Add($"?? SOLUTION: Use MessageType.File for non-text files like '{fileName}'");
                    errors.Add($"?? FILE INFO: Extension '{fileExtension}', ContentType '{contentType}'");
                }
                break;

            // Handle other message types
            case MessageType.Gif:
                if (contentType != "image/gif")
                {
                    isValid = false;
                    errors.Add($"? REJECTED: MessageType is 'Gif' but content type is '{contentType}'. Expected 'image/gif'.");
                    errors.Add($"?? SOLUTION: Use MessageType.Image for other image types or MessageType.File for documents.");
                }
                break;

            case MessageType.Sticker:
                if (!contentType.StartsWith("image/"))
                {
                    isValid = false;
                    errors.Add($"? REJECTED: MessageType is 'Sticker' but content type is '{contentType}'. Expected image/* content type.");
                    errors.Add($"?? SOLUTION: Use MessageType.File for non-image files.");
                }
                break;
        }

        return new FileValidationResult
        {
            IsValid = isValid, // S? d?ng isValid thay vì check error messages
            Errors = errors
        };
    }

    /// <summary>
    /// Auto-detect actual file type using multiple strategies
    /// </summary>
    private async Task<string> AutoDetectFileTypeAsync(IFormFile file)
    {
        try
        {
            var fileName = file.FileName.ToLowerInvariant();
            var contentType = file.ContentType.ToLowerInvariant();
            var extension = Path.GetExtension(fileName);

            // Strategy 1: File signature detection (most reliable)
            var signatureType = await DetectByFileSignatureAsync(file);
            if (!string.IsNullOrEmpty(signatureType) && signatureType != "unknown")
            {
                return signatureType;
            }

            // Strategy 2: Content-Type header detection
            var contentTypeResult = DetectByContentTypeHeader(contentType);
            if (!string.IsNullOrEmpty(contentTypeResult) && contentTypeResult != "unknown")
            {
                return contentTypeResult;
            }

            // Strategy 3: File extension detection (fallback)
            return DetectByFileExtension(extension);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in auto file type detection for {FileName}, using fallback", file.FileName);
            return "document"; // Safe fallback
        }
    }

    /// <summary>
    /// Map detected file type to MessageType enum
    /// </summary>
    private MessageType MapDetectedToMessageType(string detectedType)
    {
        return detectedType.ToLowerInvariant() switch
        {
            "image" => MessageType.Image,
            "gif" => MessageType.Gif,
            "video" => MessageType.Video,
            "audio" => MessageType.Audio,
            "voicenote" => MessageType.VoiceNote,
            "document" => MessageType.File,
            "archive" => MessageType.File,
            _ => MessageType.File
        };
    }

    /// <summary>
    /// Determine optimal message type with smart override logic
    /// </summary>
    private MessageType DetermineOptimalMessageType(MessageType clientType, MessageType detectedType, IFormFile file)
    {
        // Rule 1: If client is generic "File", always use detected type
        if (clientType == MessageType.File)
        {
            _logger.LogDebug("Client used generic 'File' type, always using detected type: {DetectedType}", detectedType);
            return detectedType;
        }

        // Rule 2: If client and detected types match, use client preference
        if (clientType == detectedType)
        {
            _logger.LogDebug("Client type matches detected type: {Type}", clientType);
            return clientType;
        }

        // Rule 3: If detection suggests more specific type, use it but log
        if (ShouldOverrideClientType(clientType, detectedType, file))
        {
            _logger.LogWarning(
                "Smart override: Client specified {ClientType} but file {FileName} detected as {DetectedType}. Using detected type for better organization.",
                clientType, file.FileName, detectedType
            );
            return detectedType;
        }

        // Rule 4: Respect client preference if no strong reason to override
        _logger.LogDebug("Respecting client preference: {ClientType} for file {FileName}", clientType, file.FileName);
        return clientType;
    }

    /// <summary>
    /// Determine if client type should be overridden for better organization
    /// </summary>
    private bool ShouldOverrideClientType(MessageType clientType, MessageType detectedType, IFormFile file)
    {
        // Only override if detected type provides better organization/storage
        return detectedType switch
        {
            MessageType.Image when clientType != MessageType.Image => true,  // Images should go to Cloudinary
            MessageType.Video when clientType != MessageType.Video => true,  // Videos should go to Cloudinary  
            MessageType.Gif when clientType != MessageType.Gif => true,      // GIFs need special handling
            _ => false  // Don't override otherwise
        };
    }

    /// <summary>
    /// Detect file type by signature analysis
    /// </summary>
    private async Task<string> DetectByFileSignatureAsync(IFormFile file)
    {
        try
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[8];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            stream.Position = 0;

            // JPEG signature
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8)
                return "image";

            // PNG signature  
            if (buffer.Length >= 4 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47)
                return "image";

            // GIF signature
            if (buffer.Length >= 3 && buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46)
                return "gif";

            // PDF signature
            if (buffer.Length >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
                return "document";

            // ZIP signature
            if (buffer.Length >= 4 && buffer[0] == 0x50 && buffer[1] == 0x4B && buffer[2] == 0x03 && buffer[3] == 0x04)
                return "archive";

            return "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>
    /// Detect file type by Content-Type header
    /// </summary>
    private string DetectByContentTypeHeader(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return "unknown";

        return contentType switch
        {
            var ct when ct.StartsWith("image/gif") => "gif",
            var ct when ct.StartsWith("image/") => "image",
            var ct when ct.StartsWith("video/") => "video",
            var ct when ct.StartsWith("audio/") => "audio",
            "application/pdf" => "document",
            var ct when ct.Contains("word") || ct.Contains("excel") || ct.Contains("powerpoint") => "document",
            var ct when ct.Contains("zip") || ct.Contains("rar") || ct.Contains("7z") => "archive",
            "text/plain" => "document",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Detect file type by extension (fallback)
    /// </summary>
    private string DetectByFileExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" => "image",
            ".gif" => "gif",
            ".mp4" or ".avi" or ".mov" or ".webm" or ".mkv" => "video",
            ".mp3" or ".wav" or ".ogg" or ".m4a" or ".aac" => "audio",
            ".pdf" or ".doc" or ".docx" or ".txt" or ".rtf" => "document",
            ".xls" or ".xlsx" or ".csv" => "document",
            ".ppt" or ".pptx" => "document",
            ".zip" or ".rar" or ".7z" or ".tar" or ".gz" => "archive",
            _ => "document"  // Default to document for unknown types
        };
    }
    #endregion
}
