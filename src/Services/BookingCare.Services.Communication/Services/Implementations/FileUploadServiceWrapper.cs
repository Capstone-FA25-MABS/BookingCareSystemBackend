using BookingCare.Services.Communication.Enums;
using BookingCare.Services.Communication.Services.Interfaces;

// Type aliases to resolve ambiguous references
using CommFileUploadResult = BookingCare.Services.Communication.Services.Interfaces.FileUploadResult;
using CommPresignedUrlResult = BookingCare.Services.Communication.Services.Interfaces.PresignedUrlResult;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Wrapper service to maintain backward compatibility with existing Communication Service APIs
/// Delegates to HybridFileUploadService while preserving existing interface
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
    /// Upload file - delegates to hybrid service with smart routing
    /// </summary>
    public async Task<CommFileUploadResult> UploadFileAsync(IFormFile file, string userId, MessageType messageType)
    {
        _logger.LogInformation("FileUploadServiceWrapper: Delegating upload to hybrid service for user {UserId}, file {FileName}",
            userId, file.FileName);

        return await _hybridService.UploadFileAsync(file, userId, messageType);
    }

    /// <summary>
    /// Upload multiple files - delegates to hybrid service
    /// </summary>
    public async Task<List<CommFileUploadResult>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string userId, MessageType messageType)
    {
        _logger.LogInformation("FileUploadServiceWrapper: Delegating multiple upload to hybrid service for user {UserId}", userId);

        return await _hybridService.UploadMultipleFilesAsync(files, userId, messageType);
    }

    /// <summary>
    /// Generate presigned URL - delegates to hybrid service
    /// </summary>
    public async Task<CommPresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string userId, MessageType messageType)
    {
        _logger.LogInformation("FileUploadServiceWrapper: Delegating presigned URL generation to hybrid service for user {UserId}, file {FileName}",
            userId, fileName);

        return await _hybridService.GeneratePresignedUrlAsync(fileName, contentType, userId, messageType);
    }

    /// <summary>
    /// Delete file - delegates to hybrid service
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        _logger.LogInformation("FileUploadServiceWrapper: Delegating file deletion to hybrid service for URL {FileUrl}", fileUrl);

        return await _hybridService.DeleteFileAsync(fileUrl);
    }

    /// <summary>
    /// Generate thumbnail - delegates to hybrid service  
    /// </summary>
    public async Task<string?> GenerateThumbnailAsync(string originalUrl)
    {
        _logger.LogInformation("FileUploadServiceWrapper: Delegating thumbnail generation to hybrid service for URL {OriginalUrl}", originalUrl);

        return await _hybridService.GenerateThumbnailAsync(originalUrl);
    }

    /// <summary>
    /// Validate file - delegates to hybrid service
    /// </summary>
    public async Task<FileValidationResult> ValidateFileAsync(IFormFile file, MessageType messageType)
    {
        _logger.LogDebug("FileUploadServiceWrapper: Delegating file validation to hybrid service for file {FileName}", file.FileName);

        return await _hybridService.ValidateFileAsync(file, messageType);
    }
}