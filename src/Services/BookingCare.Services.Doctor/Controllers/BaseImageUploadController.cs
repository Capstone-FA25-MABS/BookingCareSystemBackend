using BookingCare.Shared.Common.Controllers;
using BookingCare.Shared.FileUpload.Services;
using BookingCare.Shared.FileUpload.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookingCare.Services.Doctor.Controllers;

/// <summary>
/// Base controller for handling image uploads with common functionality
/// </summary>
public abstract class BaseImageUploadController : BaseApiController
{
    protected readonly FileUploadOrchestrator _uploadOrchestrator;
    protected readonly ILogger _logger;

    protected BaseImageUploadController(
        FileUploadOrchestrator uploadOrchestrator,
        ILogger logger)
    {
        _uploadOrchestrator = uploadOrchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Handle image upload with configurable parameters
    /// </summary>
    /// <param name="imageFile">Image file to upload</param>
    /// <param name="request">Request object to set image URL</param>
    /// <param name="folder">Folder name for upload</param>
    /// <param name="entityType">Entity type for upload</param>
    /// <param name="successMessage">Success message for upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>BadRequest if upload fails, null if successful</returns>
    protected async Task<IActionResult?> HandleImageUploadAsync(
        IFormFile? imageFile,
        dynamic request,
        string folder,
        string entityType,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (imageFile == null) return null;

        var config = new FileUploadConfig
        {
            AllowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" },
            MaxSizeInMB = 5,
            Folder = folder,
            SuccessMessage = successMessage,
            EntityType = entityType
        };

        var uploadResult = await _uploadOrchestrator.UploadFileAsync(imageFile, config, Guid.Empty, _logger, cancellationToken);

        if (!uploadResult.Success)
        {
            return BadRequest($"Tải lên hình ảnh thất bại: {uploadResult.ErrorMessage}");
        }

        // Set the image URL from upload result - use CloudFront URL for public access
        request.ImageUrl = uploadResult.UploadResult!.CloudFrontUrl ?? uploadResult.UploadResult!.FileUrl;
        return null;
    }

    /// <summary>
    /// Handle image deletion with configurable parameters
    /// </summary>
    /// <param name="imageUrl">Image URL to delete</param>
    /// <param name="expectedFolder">Expected folder for the image</param>
    /// <param name="entityType">Entity type for deletion</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful or not needed, false if deletion failed</returns>
    protected async Task<bool> HandleImageDeletionAsync(
        string imageUrl,
        string expectedFolder,
        string entityType,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(imageUrl)) return true;

        var deleteConfig = new FileDeletionConfig
        {
            FileUrl = imageUrl,
            ExpectedFolder = expectedFolder,
            EntityType = entityType
        };

        var deleteResult = await _uploadOrchestrator.DeleteFileAsync(deleteConfig, Guid.Empty, _logger, cancellationToken);

        if (!deleteResult.Success)
        {
            _logger.LogWarning("Failed to delete image: {ErrorMessage}", deleteResult.ErrorMessage);
            return false;
        }

        return true;
    }
}
