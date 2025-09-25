using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.FileUpload.Models;

// Type aliases to resolve ambiguous references
using CommFileUploadResult = BookingCare.Services.Communication.Services.Interfaces.FileUploadResult;
using CommPresignedUrlResult = BookingCare.Services.Communication.Services.Interfaces.PresignedUrlResult;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Hybrid File Upload Service interface - combines Cloudinary and AWS S3
/// Provides backward compatibility with existing Communication Service APIs
/// </summary>
public interface IHybridFileUploadService
{
    /// <summary>
    /// Upload file using hybrid approach based on MessageType
    /// - Documents, general files ? AWS S3 + CloudFront
    /// - Images, videos requiring processing ? Cloudinary
    /// </summary>
    Task<CommFileUploadResult> UploadFileAsync(IFormFile file, string userId, MessageType messageType);

    /// <summary>
    /// Upload multiple files using hybrid approach
    /// </summary>
    Task<List<CommFileUploadResult>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string userId, MessageType messageType);

    /// <summary>
    /// Generate presigned URL - routed to appropriate provider
    /// </summary>
    Task<CommPresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string userId, MessageType messageType);

    /// <summary>
    /// Delete file from appropriate cloud storage
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Generate thumbnail - primarily uses Cloudinary for rich processing
    /// </summary>
    Task<string?> GenerateThumbnailAsync(string originalUrl);

    /// <summary>
    /// Validate file - enhanced validation with both providers' constraints
    /// </summary>
    Task<FileValidationResult> ValidateFileAsync(IFormFile file, MessageType messageType);

    /// <summary>
    /// NEW: Upload file to AWS S3 directly (for documents, medical files)
    /// </summary>
    Task<CommFileUploadResult> UploadToS3Async(IFormFile file, string userId, MessageType messageType, string? customFolder = null);

    /// <summary>
    /// NEW: Upload file to Cloudinary directly (for rich media processing)
    /// </summary>
    Task<CommFileUploadResult> UploadToCloudinaryAsync(IFormFile file, string userId, MessageType messageType);

    /// <summary>
    /// NEW: Get CloudFront URL for S3 files
    /// </summary>
    string GetCloudFrontUrl(string s3Key);

    /// <summary>
    /// NEW: Check if file exists in either storage
    /// </summary>
    Task<bool> FileExistsAsync(string fileUrl);
}