using BookingCare.Shared.Common.Helpers;
using BookingCare.Shared.FileUpload.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BookingCare.Shared.FileUpload.Services;

/// <summary>
/// Configuration for file upload
/// </summary>
public class FileUploadConfig
{
    public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    public int MaxSizeInMB { get; set; }
    public string Folder { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = "File uploaded successfully";
    public string EntityType { get; set; } = "file"; // For logging: "attachment", "avatar", etc.
}

/// <summary>
/// Configuration for file deletion
/// </summary>
public class FileDeletionConfig
{
    public string FileUrl { get; set; } = string.Empty;
    public string ExpectedFolder { get; set; } = string.Empty;
    public string SuccessMessage { get; set; } = "File deleted successfully";
    public string EntityType { get; set; } = "file"; // For logging
}

/// <summary>
/// Result of file upload orchestration
/// </summary>
public class FileUploadOrchestratorResult
{
    public bool Success { get; set; }
    public FileUploadResult? UploadResult { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result of file deletion orchestration
/// </summary>
public class FileDeletionOrchestratorResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Orchestrator service for file upload and deletion operations
/// </summary>
public class FileUploadOrchestrator
{
    private readonly IFileUploadService _fileUploadService;

    public FileUploadOrchestrator(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// Orchestrates file upload with validation and error handling
    /// </summary>
    public async Task<FileUploadOrchestratorResult> UploadFileAsync(
        IFormFile file,
        FileUploadConfig config,
        Guid accountId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            if (!FileValidationHelper.ValidateFile(file, config.AllowedExtensions, config.MaxSizeInMB, out var errorMessage))
            {
                return new FileUploadOrchestratorResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            // Upload to S3
            var request = new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = config.Folder,
                GenerateUniqueFileName = true
            };

            var result = await _fileUploadService.UploadFileAsync(request, cancellationToken);

            if (!result.Success)
            {
                logger.LogError("Failed to upload {EntityType} for account {AccountId}: {Error}",
                    config.EntityType, accountId, result.ErrorMessage);

                return new FileUploadOrchestratorResult
                {
                    Success = false,
                    ErrorMessage = result.ErrorMessage
                };
            }

            logger.LogInformation("{EntityType} uploaded successfully for account {AccountId}",
                config.EntityType, accountId);

            return new FileUploadOrchestratorResult
            {
                Success = true,
                UploadResult = result
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading {EntityType}", config.EntityType);

            return new FileUploadOrchestratorResult
            {
                Success = false,
                ErrorMessage = "An internal server error occurred during file upload"
            };
        }
    }

    /// <summary>
    /// Orchestrates file deletion with validation and error handling
    /// </summary>
    public async Task<FileDeletionOrchestratorResult> DeleteFileAsync(
        FileDeletionConfig config,
        Guid accountId,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(config.FileUrl))
            {
                return new FileDeletionOrchestratorResult
                {
                    Success = false,
                    ErrorMessage = "File URL is required"
                };
            }

            // Extract S3 key from URL
            var s3Key = Helpers.FileUploadHelper.ExtractS3KeyFromUrl(config.FileUrl, config.ExpectedFolder);
            if (string.IsNullOrEmpty(s3Key))
            {
                return new FileDeletionOrchestratorResult
                {
                    Success = false,
                    ErrorMessage = "Invalid file URL"
                };
            }

            // Delete from S3
            var deleted = await _fileUploadService.DeleteFileAsync(s3Key, cancellationToken);
            if (!deleted)
            {
                logger.LogWarning("Failed to delete {EntityType} from S3: {S3Key}", config.EntityType, s3Key);

                return new FileDeletionOrchestratorResult
                {
                    Success = false,
                    ErrorMessage = "Failed to delete file from storage"
                };
            }

            logger.LogInformation("{EntityType} deleted successfully for account {AccountId}",
                config.EntityType, accountId);

            return new FileDeletionOrchestratorResult
            {
                Success = true,
                Message = config.SuccessMessage
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting {EntityType}", config.EntityType);

            return new FileDeletionOrchestratorResult
            {
                Success = false,
                ErrorMessage = "An internal server error occurred during file deletion"
            };
        }
    }
}

