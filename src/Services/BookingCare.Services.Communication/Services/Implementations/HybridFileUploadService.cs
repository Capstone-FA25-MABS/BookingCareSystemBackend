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
/// Hybrid File Upload Service - combines AWS S3 and Cloudinary
/// Routes files to appropriate storage based on MessageType and requirements
/// Maintains backward compatibility with existing Communication Service APIs
/// </summary>
public class HybridFileUploadService : IHybridFileUploadService
{
    private readonly S3FileUploadService _s3Service;              // AWS S3 + CloudFront
    private readonly ICloudStorageProvider _cloudinaryProvider;  // Cloudinary for rich media
    private readonly FileUploadConfiguration _config;
    private readonly ILogger<HybridFileUploadService> _logger;
    private readonly IMapper _mapper;

    public HybridFileUploadService(
        S3FileUploadService s3Service,
        ICloudStorageProvider cloudinaryProvider,
        IOptions<FileUploadConfiguration> config,
        ILogger<HybridFileUploadService> logger,
        IMapper mapper)
    {
        _s3Service = s3Service;
        _cloudinaryProvider = cloudinaryProvider;
        _config = config.Value;
        _logger = logger;
        _mapper = mapper;
    }

    /// <summary>
    /// Smart routing: Documents ? S3, Rich media ? Cloudinary
    /// </summary>
    public async Task<CommFileUploadResult> UploadFileAsync(IFormFile file, string userId, MessageType messageType)
    {
        try
        {
            _logger.LogInformation("Starting hybrid file upload for user {UserId}, file {FileName}, type {MessageType}",
                userId, file.FileName, messageType);

            // Route based on MessageType and provider preference
            if (ShouldUseS3(messageType, file.ContentType))
            {
                return await UploadToS3Async(file, userId, messageType);
            }
            else
            {
                return await UploadToCloudinaryAsync(file, userId, messageType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in hybrid file upload for user {UserId}, file {FileName}", userId, file.FileName);
            throw;
        }
    }

    /// <summary>
    /// Upload multiple files with smart routing
    /// </summary>
    public async Task<List<CommFileUploadResult>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string userId, MessageType messageType)
    {
        var tasks = files.Select(file => UploadFileAsync(file, userId, messageType));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Generate presigned URL with provider routing
    /// </summary>
    public async Task<CommPresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string userId, MessageType messageType)
    {
        if (ShouldUseS3(messageType, contentType))
        {
            var request = new PresignedUrlRequest
            {
                FileName = fileName,
                ContentType = contentType,
                Folder = GenerateS3Folder(userId, messageType),
                ExpiryHours = 1
            };

            var s3Result = await _s3Service.GeneratePresignedUploadUrlAsync(request);

            // Map to Communication Service format
            return new CommPresignedUrlResult
            {
                UploadUrl = s3Result.UploadUrl,
                FinalUrl = s3Result.CloudFrontUrl,
                ExpiresAt = s3Result.ExpiresAt,
                Headers = new Dictionary<string, string>()
            };
        }
        else
        {
            return await _cloudinaryProvider.GeneratePresignedUrlAsync(
                fileName, contentType, GenerateCloudinaryFolder(userId, messageType), TimeSpan.FromHours(1));
        }
    }

    /// <summary>
    /// Delete file from appropriate storage
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
            return await _cloudinaryProvider.DeleteFileAsync(fileUrl);
        }
    }

    /// <summary>
    /// Generate thumbnail (primarily Cloudinary for rich processing)
    /// </summary>
    public async Task<string?> GenerateThumbnailAsync(string originalUrl)
    {
        // For now, delegate to Cloudinary for thumbnail generation
        // Future: Could add S3 + Lambda for thumbnail generation
        if (IsCloudinaryUrl(originalUrl))
        {
            return await GenerateCloudinaryThumbnailAsync(originalUrl);
        }

        _logger.LogWarning("Thumbnail generation not yet supported for S3 URLs: {Url}", originalUrl);
        return null;
    }

    /// <summary>
    /// Enhanced validation considering both providers
    /// </summary>
    public async Task<FileValidationResult> ValidateFileAsync(IFormFile file, MessageType messageType)
    {
        var errors = new List<string>();

        // Basic validation
        if (file == null || file.Length == 0)
        {
            errors.Add("File is required");
            return new FileValidationResult { IsValid = false, Errors = errors };
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
            errors.Add($"File size ({file.Length:N0} bytes) exceeds maximum allowed ({constraints.MaxSizeBytes:N0} bytes)");
        }

        // MIME type validation
        if (!constraints.AllowedMimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
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
            DetectedMimeType = file.ContentType
        };
    }

    /// <summary>
    /// Upload directly to AWS S3
    /// </summary>
    public async Task<CommFileUploadResult> UploadToS3Async(IFormFile file, string userId, MessageType messageType, string? customFolder = null)
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
                ["ServiceName"] = "Communication"
            }
        };

        var s3Result = await _s3Service.UploadFileAsync(request);

        // Map to Communication Service format
        return new CommFileUploadResult
        {
            Url = s3Result.CloudFrontUrl, // Use CloudFront URL for better performance
            FileName = s3Result.FileName,
            Size = file.Length,
            MimeType = file.ContentType,
            ThumbnailUrl = null, // S3 doesn't auto-generate thumbnails
            Width = null,
            Height = null,
            Duration = null
        };
    }

    /// <summary>
    /// Upload directly to Cloudinary
    /// </summary>
    public async Task<CommFileUploadResult> UploadToCloudinaryAsync(IFormFile file, string userId, MessageType messageType)
    {
        using var stream = file.OpenReadStream();
        var folder = GenerateCloudinaryFolder(userId, messageType);

        var uploadUrl = await _cloudinaryProvider.UploadFileAsync(stream, file.FileName, file.ContentType, folder);

        // Get file info for images/videos
        var (width, height, duration) = await GetMediaInfoAsync(file);

        var result = new CommFileUploadResult
        {
            Url = uploadUrl,
            FileName = file.FileName,
            Size = file.Length,
            MimeType = file.ContentType,
            Width = width,
            Height = height,
            Duration = duration
        };

        // Generate thumbnail if needed
        if (ShouldGenerateThumbnail(messageType, file.ContentType))
        {
            result.ThumbnailUrl = await GenerateCloudinaryThumbnailAsync(uploadUrl);
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
    /// Check if file exists in either storage
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
            return await _cloudinaryProvider.FileExistsAsync(fileUrl);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Determine which storage to use based on MessageType and content
    /// </summary>
    private bool ShouldUseS3(MessageType messageType, string contentType)
    {
        // Route documents and general files to S3
        if (messageType == MessageType.File)
            return true;

        // Route audio files to S3 (better for large files)
        if (messageType == MessageType.Audio || messageType == MessageType.VoiceNote)
            return true;

        // Use Cloudinary for images and videos requiring processing
        if (messageType == MessageType.Image || messageType == MessageType.Video)
            return false;

        // Default to S3 for unknown types
        return true;
    }

    private bool IsS3Url(string url)
    {
        return url.Contains("cloudfront.net") || url.Contains("amazonaws.com");
    }

    private bool IsCloudinaryUrl(string url)
    {
        return url.Contains("cloudinary.com");
    }

    private string ExtractS3KeyFromUrl(string url)
    {
        // Implementation to extract S3 key from CloudFront or S3 URL
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }

    private string GenerateS3Folder(string userId, MessageType messageType)
    {
        return $"communication/{userId}/{messageType.ToString().ToLower()}";
    }

    private string GenerateCloudinaryFolder(string userId, MessageType messageType)
    {
        return $"communication/{userId}/{messageType.ToString().ToLower()}";
    }

    private bool ShouldGenerateThumbnail(MessageType messageType, string contentType)
    {
        if (messageType != MessageType.Image && messageType != MessageType.Video)
            return false;

        if (!_config.Constraints.TryGetValue(messageType.ToString(), out var constraints))
            return false;

        return constraints.GenerateThumbnail;
    }

    private async Task<string?> GenerateCloudinaryThumbnailAsync(string originalUrl)
    {
        try
        {
            // Extract public_id from Cloudinary URL
            var publicId = ExtractCloudinaryPublicId(originalUrl);
            if (string.IsNullOrEmpty(publicId))
                return null;

            // Generate thumbnail URL using Cloudinary transformation
            var baseUrl = originalUrl.Substring(0, originalUrl.LastIndexOf('/') + 1);
            var thumbnailUrl = $"{baseUrl}c_thumb,w_300,h_300/{publicId}";

            return thumbnailUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Cloudinary thumbnail for {Url}", originalUrl);
            return null;
        }
    }

    private string? ExtractCloudinaryPublicId(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Find upload segment and extract public_id
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i] == "upload" && i + 1 < segments.Length)
                {
                    var publicIdWithFormat = string.Join("/", segments.Skip(i + 1));

                    // Remove version if present
                    if (publicIdWithFormat.StartsWith("v") && publicIdWithFormat.Length > 1 && char.IsDigit(publicIdWithFormat[1]))
                    {
                        var versionEnd = publicIdWithFormat.IndexOf('/');
                        if (versionEnd > 0)
                        {
                            publicIdWithFormat = publicIdWithFormat.Substring(versionEnd + 1);
                        }
                    }

                    // Remove file extension
                    var lastDotIndex = publicIdWithFormat.LastIndexOf('.');
                    return lastDotIndex > 0 ? publicIdWithFormat.Substring(0, lastDotIndex) : publicIdWithFormat;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<(int? width, int? height, int? duration)> GetMediaInfoAsync(IFormFile file)
    {
        // Simple implementation - could be enhanced with actual media analysis
        await Task.CompletedTask;
        return (null, null, null);
    }

    private async Task ValidateMediaFileAsync(IFormFile file, MessageTypeConstraints constraints, List<string> errors)
    {
        // Enhanced media validation could be added here
        await Task.CompletedTask;
    }

    #endregion
}