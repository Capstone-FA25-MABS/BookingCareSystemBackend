using BookingCare.Shared.FileUpload.Models;

namespace BookingCare.Shared.FileUpload.Services;

public interface IFileUploadService
{
    /// <summary>
    /// Upload a single file to S3
    /// </summary>
    Task<FileUploadResult> UploadFileAsync(FileUploadRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload multiple files to S3
    /// </summary>
    Task<MultipleFileUploadResult> UploadMultipleFilesAsync(IEnumerable<FileUploadRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate presigned URL for direct upload to S3
    /// </summary>
    Task<PresignedUrlResult> GeneratePresignedUploadUrlAsync(PresignedUrlRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate presigned URL for downloading from S3
    /// </summary>
    Task<PresignedUrlResult> GeneratePresignedDownloadUrlAsync(string s3Key, int expiryHours = 1, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from S3
    /// </summary>
    Task<bool> DeleteFileAsync(string s3Key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete multiple files from S3
    /// </summary>
    Task<Dictionary<string, bool>> DeleteMultipleFilesAsync(IEnumerable<string> s3Keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if file exists in S3
    /// </summary>
    Task<bool> FileExistsAsync(string s3Key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get file info from S3
    /// </summary>
    Task<FileUploadResult?> GetFileInfoAsync(string s3Key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate CloudFront cache for specific files
    /// </summary>
    Task<bool> InvalidateCloudFrontCacheAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get CloudFront URL for an S3 key
    /// </summary>
    string GetCloudFrontUrl(string s3Key);

    /// <summary>
    /// Validate file before upload
    /// </summary>
    (bool IsValid, string? ErrorMessage) ValidateFile(string fileName, long fileSize, string contentType);
}