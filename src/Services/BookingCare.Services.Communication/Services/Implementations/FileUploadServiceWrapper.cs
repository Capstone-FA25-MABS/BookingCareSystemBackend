using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Services.Interfaces;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// S3-only FileUploadService wrapper - delegates to HybridFileUploadService in S3-only mode
/// Maintains backward compatibility with existing MessageService
/// </summary>
public class FileUploadServiceWrapper : IFileUploadService
{
    private readonly IHybridFileUploadService _hybridService;
    private readonly ILogger<FileUploadServiceWrapper> _logger;

    public FileUploadServiceWrapper(
        IHybridFileUploadService hybridService,
        ILogger<FileUploadServiceWrapper> logger)
    {
        _hybridService = hybridService;
        _logger = logger;
    }

    /// <summary>
    /// Upload file - delegates to S3-only HybridFileUploadService
    /// </summary>
    public async Task<FileUploadResult> UploadFileAsync(IFormFile file, string userId, MessageType messageType)
    {
        _logger.LogInformation("FileUploadService wrapper: routing to S3-only HybridFileUploadService");

        var hybridResult = await _hybridService.UploadFileAsync(file, userId, messageType);

        // Map HybridFileUploadResult to FileUploadResult
        return new FileUploadResult
        {
            Url = hybridResult.Url,
            FileName = hybridResult.FileName,
            Size = hybridResult.Size,
            MimeType = hybridResult.MimeType,
            ThumbnailUrl = hybridResult.ThumbnailUrl,
            Width = hybridResult.Width,
            Height = hybridResult.Height,
            Duration = hybridResult.Duration
        };
    }

    /// <summary>
    /// Upload multiple files - delegates to S3-only HybridFileUploadService
    /// </summary>
    public async Task<List<FileUploadResult>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string userId, MessageType messageType)
    {
        _logger.LogInformation("FileUploadService wrapper: routing multiple files to S3-only HybridFileUploadService");

        var hybridResults = await _hybridService.UploadMultipleFilesAsync(files, userId, messageType);

        // Map results
        return hybridResults.Select(hr => new FileUploadResult
        {
            Url = hr.Url,
            FileName = hr.FileName,
            Size = hr.Size,
            MimeType = hr.MimeType,
            ThumbnailUrl = hr.ThumbnailUrl,
            Width = hr.Width,
            Height = hr.Height,
            Duration = hr.Duration
        }).ToList();
    }

    /// <summary>
    /// Generate presigned URL - delegates to S3-only HybridFileUploadService
    /// </summary>
    public async Task<PresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string userId, MessageType messageType)
    {
        _logger.LogInformation("FileUploadService wrapper: generating S3 presigned URL via HybridFileUploadService");

        var hybridResult = await _hybridService.GeneratePresignedUrlAsync(fileName, contentType, userId, messageType);

        // Map result
        return new PresignedUrlResult
        {
            UploadUrl = hybridResult.UploadUrl,
            FinalUrl = hybridResult.FinalUrl,
            ExpiresAt = hybridResult.ExpiresAt,
            Headers = hybridResult.Headers
        };
    }

    /// <summary>
    /// Delete file - delegates to S3-only HybridFileUploadService
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        _logger.LogInformation("FileUploadService wrapper: deleting file via S3-only HybridFileUploadService");
        return await _hybridService.DeleteFileAsync(fileUrl);
    }

    /// <summary>
    /// Generate thumbnail - delegates to S3-only HybridFileUploadService
    /// </summary>
    public async Task<string?> GenerateThumbnailAsync(string originalUrl)
    {
        _logger.LogInformation("FileUploadService wrapper: generating thumbnail via S3-only HybridFileUploadService");
        return await _hybridService.GenerateThumbnailAsync(originalUrl);
    }

    /// <summary>
    /// Validate file - delegates to S3-only HybridFileUploadService
    /// </summary>
    public async Task<FileValidationResult> ValidateFileAsync(IFormFile file, MessageType messageType)
    {
        return await _hybridService.ValidateFileAsync(file, messageType);
    }
}