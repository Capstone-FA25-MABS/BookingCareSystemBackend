using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Configuration;
using BookingCare.Services.Communication.Enums;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Implementation của File Upload service với Cloudinary
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly ICloudStorageProvider _storageProvider;
    private readonly FileUploadConfiguration _config;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(
        ICloudStorageProvider storageProvider,
        IOptions<FileUploadConfiguration> config,
        ILogger<FileUploadService> logger)
    {
        _storageProvider = storageProvider;
        _config = config.Value;
        _logger = logger;
    }

    /// <summary>
    /// Upload single file lên cloud storage
    /// </summary>
    public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string userId, MessageType messageType)
    {
        try
        {
            _logger.LogInformation("Starting file upload for user {UserId}, file {FileName}, type {MessageType}",
                userId, file.FileName, messageType);

            // Validate file
            var validation = await ValidateFileAsync(file, messageType);
            if (!validation.IsValid)
            {
                throw new ArgumentException($"File validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Generate folder path
            var folder = GenerateFolder(userId, messageType);

            // Get file info
            var fileInfo = await GetFileInfoAsync(file);

            // Upload to cloud storage
            using var stream = file.OpenReadStream();
            var uploadUrl = await _storageProvider.UploadFileAsync(stream, file.FileName, file.ContentType, folder);

            // Create result
            var result = new FileUploadResult
            {
                Url = uploadUrl,
                FileName = file.FileName,
                Size = file.Length,
                MimeType = validation.DetectedMimeType ?? file.ContentType,
                Width = fileInfo.Width,
                Height = fileInfo.Height,
                Duration = fileInfo.Duration
            };

            // Generate thumbnail if needed
            if (ShouldGenerateThumbnail(messageType, file.ContentType))
            {
                result.ThumbnailUrl = await GenerateThumbnailAsync(uploadUrl);
            }

            _logger.LogInformation("File uploaded successfully: {Url}", uploadUrl);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} for user {UserId}", file.FileName, userId);
            throw new InvalidOperationException($"Failed to upload file '{file.FileName}' for user '{userId}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Upload multiple files cùng lúc
    /// </summary>
    public async Task<List<FileUploadResult>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string userId, MessageType messageType)
    {
        var results = new List<FileUploadResult>();
        var tasks = new List<Task<FileUploadResult>>();

        foreach (var file in files)
        {
            tasks.Add(UploadFileAsync(file, userId, messageType));
        }

        var uploadResults = await Task.WhenAll(tasks);
        results.AddRange(uploadResults);

        return results;
    }

    /// <summary>
    /// Generate presigned URL cho direct upload
    /// </summary>
    public async Task<PresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string userId, MessageType messageType)
    {
        try
        {
            _logger.LogInformation("Generating presigned URL for {FileName}, user {UserId}", fileName, userId);

            // Validate file constraints
            var constraints = GetConstraints(messageType);
            if (!IsContentTypeAllowed(contentType, constraints))
            {
                throw new ArgumentException($"Content type {contentType} not allowed for message type {messageType}");
            }

            var folder = GenerateFolder(userId, messageType);
            var expiration = TimeSpan.FromHours(1); // 1 hour expiration

            var result = await _storageProvider.GeneratePresignedUrlAsync(fileName, contentType, folder, expiration);

            _logger.LogInformation("Presigned URL generated successfully for {FileName}", fileName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL for {FileName}", fileName);
            throw new InvalidOperationException($"Failed to generate presigned URL for file '{fileName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Xóa file từ cloud storage
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            _logger.LogInformation("Deleting file: {FileUrl}", fileUrl);

            var result = await _storageProvider.DeleteFileAsync(fileUrl);

            if (result)
            {
                _logger.LogInformation("File deleted successfully: {FileUrl}", fileUrl);
            }
            else
            {
                _logger.LogWarning("Failed to delete file: {FileUrl}", fileUrl);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FileUrl}", fileUrl);
            return false;
        }
    }

    /// <summary>
    /// Generate thumbnail cho images/videos
    /// </summary>
    public async Task<string?> GenerateThumbnailAsync(string originalUrl)
    {
        try
        {
            _logger.LogInformation("Generating thumbnail for: {OriginalUrl}", originalUrl);

            // For Cloudinary, we can generate thumbnails on-the-fly by URL transformation
            if (originalUrl.Contains("cloudinary.com"))
            {
                var thumbnailUrl = GenerateCloudinaryThumbnailUrl(originalUrl);
                _logger.LogInformation("Thumbnail generated: {ThumbnailUrl}", thumbnailUrl);
                return thumbnailUrl;
            }

            // For other providers, implement custom thumbnail generation
            _logger.LogWarning("Thumbnail generation not implemented for non-Cloudinary URLs");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for: {OriginalUrl}", originalUrl);
            await Task.Delay(1);
            return null;
        }
    }

    /// <summary>
    /// Validate file type và size
    /// </summary>
    public async Task<FileValidationResult> ValidateFileAsync(IFormFile file, MessageType messageType)
    {
        var result = new FileValidationResult { IsValid = true };

        try
        {
            // Get constraints cho message type
            var constraints = GetConstraints(messageType);

            // Validate file size
            if (file.Length > constraints.MaxSizeBytes)
            {
                result.IsValid = false;
                result.Errors.Add($"File size {file.Length:N0} bytes exceeds maximum allowed {constraints.MaxSizeBytes:N0} bytes");
            }

            // Detect actual MIME type
            var detectedMimeType = await DetectMimeTypeAsync(file);
            result.DetectedMimeType = detectedMimeType;

            // Validate MIME type
            if (!IsContentTypeAllowed(detectedMimeType, constraints))
            {
                result.IsValid = false;
                result.Errors.Add($"File type {detectedMimeType} is not allowed for message type {messageType}");
            }

            // Validate image dimensions
            if (detectedMimeType.StartsWith("image/") && (constraints.MaxWidth.HasValue || constraints.MaxHeight.HasValue))
            {
                var imageInfo = await GetImageInfoAsync(file);
                if (constraints.MaxWidth.HasValue && imageInfo.Width > constraints.MaxWidth.Value)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Image width {imageInfo.Width} exceeds maximum {constraints.MaxWidth}");
                }
                if (constraints.MaxHeight.HasValue && imageInfo.Height > constraints.MaxHeight.Value)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Image height {imageInfo.Height} exceeds maximum {constraints.MaxHeight}");
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating file {FileName}", file.FileName);
            result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
            return result;
        }
    }

    #region Private Helper Methods

    private MessageTypeConstraints GetConstraints(MessageType messageType)
    {
        var messageTypeKey = messageType.ToString();
        return _config.Constraints.TryGetValue(messageTypeKey, out var constraints)
            ? constraints
            : new MessageTypeConstraints { MaxSizeBytes = 10 * 1024 * 1024 }; // Default 10MB
    }

    private static bool IsContentTypeAllowed(string contentType, MessageTypeConstraints constraints)
    {
        if (!constraints.AllowedMimeTypes.Any()) return true;
        return constraints.AllowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase);
    }

    private static string GenerateFolder(string userId, MessageType messageType)
    {
        return $"communication/{messageType.ToString().ToLower()}/{DateTime.UtcNow:yyyy/MM}/{userId}";
    }

    private bool ShouldGenerateThumbnail(MessageType messageType, string contentType)
    {
        var constraints = GetConstraints(messageType);
        return constraints.GenerateThumbnail &&
               (contentType.StartsWith("image/") || contentType.StartsWith("video/"));
    }

    private static async Task<string> DetectMimeTypeAsync(IFormFile file)
    {
        // Simple MIME type detection based on file extension and content
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        // Read first few bytes to detect file signature
        using var stream = file.OpenReadStream();
        var buffer = new byte[8];
        var bytesRead = await stream.ReadAsync(buffer, 0, 8);
        stream.Position = 0;

        // Detect by file signature - only if we read enough bytes
        if (bytesRead >= 2 && IsJpeg(buffer)) return "image/jpeg";
        if (bytesRead >= 8 && IsPng(buffer)) return "image/png";
        if (bytesRead >= 6 && IsGif(buffer)) return "image/gif";
        if (bytesRead >= 4 && IsPdf(buffer)) return "application/pdf";

        // Fallback to file extension
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".avi" => "video/avi",
            ".mov" => "video/mov",
            ".mp3" => "audio/mp3",
            ".wav" => "audio/wav",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => file.ContentType
        };
    }

    private static async Task<(int? Width, int? Height, int? Duration)> GetFileInfoAsync(IFormFile file)
    {
        try
        {
            if (file.ContentType.StartsWith("image/"))
            {
                return await GetImageInfoAsync(file);
            }
            // Add video/audio info extraction if needed
            return (null, null, null);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private static async Task<(int Width, int Height, int? Duration)> GetImageInfoAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var image = await Image.LoadAsync(stream);
        return (image.Width, image.Height, null);
    }

    private static string GenerateCloudinaryThumbnailUrl(string originalUrl)
    {
        // Transform Cloudinary URL to add thumbnail transformation
        // Example: add w_300,h_300,c_fill to the URL
        if (originalUrl.Contains("/upload/"))
        {
            return originalUrl.Replace("/upload/", "/upload/w_300,h_300,c_fill,q_auto/");
        }
        return originalUrl;
    }

    private static bool IsJpeg(byte[] buffer) => buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xD8;
    private static bool IsPng(byte[] buffer) => buffer.Length >= 8 && buffer[0] == 0x89 && buffer[1] == 0x50 && buffer[2] == 0x4E && buffer[3] == 0x47;
    private static bool IsGif(byte[] buffer) => buffer.Length >= 6 && buffer[0] == 0x47 && buffer[1] == 0x49 && buffer[2] == 0x46;
    private static bool IsPdf(byte[] buffer) => buffer.Length >= 4 && buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46;

    #endregion
}