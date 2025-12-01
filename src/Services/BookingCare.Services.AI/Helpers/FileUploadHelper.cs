using BookingCare.Shared.FileUpload.Models;
using BookingCare.Shared.FileUpload.Services;
using Microsoft.AspNetCore.Http;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper service để centralize S3 file upload logic
/// Shared helper to centralize S3 file upload operations
/// </summary>
public class FileUploadHelper
{
    private readonly ILogger<FileUploadHelper> _logger;
    private readonly IFileUploadService _fileUploadService;

    public FileUploadHelper(
        ILogger<FileUploadHelper> logger,
        IFileUploadService fileUploadService)
    {
        _logger = logger;
        _fileUploadService = fileUploadService;
    }

    /// <summary>
    /// Upload file to S3 with customizable folder and metadata
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="userId">User ID for folder organization</param>
    /// <param name="fileType">Type of file (e.g., "dermatology", "lab-result")</param>
    /// <returns>CloudFront URL of uploaded file</returns>
    public async Task<string> UploadToS3Async(IFormFile file, Guid? userId, string fileType)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var folder = $"uploads/ai/{fileType}/{userId}/{timestamp}";

            using var stream = file.OpenReadStream();
            var uploadRequest = new FileUploadRequest
            {
                FileName = file.FileName,
                FileStream = stream,
                ContentType = file.ContentType,
                Folder = folder,
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    { "user-id", userId?.ToString() ?? "anonymous" },
                    { "upload-timestamp", timestamp },
                    { "file-type", fileType }
                }
            };

            var result = await _fileUploadService.UploadFileAsync(uploadRequest);

            if (result.Success)
            {
                _logger.LogInformation("File uploaded successfully to S3. CloudFront URL: {Url}", result.CloudFrontUrl);
                return result.CloudFrontUrl ?? result.FileUrl ?? string.Empty;
            }

            _logger.LogError("Failed to upload file to S3: {Error}", result.ErrorMessage);
            throw new InvalidOperationException($"Failed to upload file to S3: {result.ErrorMessage}");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error uploading file '{FileName}' to S3 for user {UserId}",
                file.FileName,
                userId ?? Guid.Empty);

            throw new InvalidOperationException(
                $"Error uploading file '{file.FileName}' to S3.",
                ex);
        }
    }
}
