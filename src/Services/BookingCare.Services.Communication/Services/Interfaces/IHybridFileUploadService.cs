using BookingCare.Services.Communication.Enums;
using BookingCare.Shared.FileUpload.Models;

// Type aliases to resolve ambiguous references
using CommFileUploadResult = BookingCare.Services.Communication.Services.Interfaces.FileUploadResult;
using CommPresignedUrlResult = BookingCare.Services.Communication.Services.Interfaces.PresignedUrlResult;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// S3-Only File Upload Service interface (formerly Hybrid)
/// Routes all files to AWS S3 + CloudFront with smart organization
/// Provides backward compatibility with existing Communication Service APIs
/// </summary>
public interface IHybridFileUploadService
{
    /// <summary>
    /// Upload file using S3-only approach with smart type detection
    /// All files go to AWS S3 + CloudFront with intelligent folder organization
    /// </summary>
    Task<CommFileUploadResult> UploadFileAsync(IFormFile file, string userId, MessageType messageType);

    /// <summary>
    /// Upload multiple files using S3-only approach
    /// </summary>
    Task<List<CommFileUploadResult>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string userId, MessageType messageType);

    /// <summary>
    /// Generate presigned URL for S3 upload
    /// </summary>
    Task<CommPresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string userId, MessageType messageType);

    /// <summary>
    /// Delete file from S3 storage
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Generate thumbnail - S3-compatible implementation (returns original URL)
    /// </summary>
    Task<string?> GenerateThumbnailAsync(string originalUrl);

    /// <summary>
    /// Validate file with enhanced S3-compatible validation
    /// </summary>
    Task<FileValidationResult> ValidateFileAsync(IFormFile file, MessageType messageType);

    /// <summary>
    /// Upload file to AWS S3 directly with smart folder organization
    /// </summary>
    Task<CommFileUploadResult> UploadToS3Async(IFormFile file, string userId, MessageType messageType, string? customFolder = null);

    /// <summary>
    /// Get CloudFront URL for S3 files
    /// </summary>
    string GetCloudFrontUrl(string s3Key);

    /// <summary>
    /// Check if file exists in S3 storage
    /// </summary>
    Task<bool> FileExistsAsync(string fileUrl);
}