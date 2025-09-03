namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho cloud storage providers
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>
    /// Upload file lên cloud storage
    /// </summary>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder);

    /// <summary>
    /// T?o presigned URL cho direct upload
    /// </summary>
    Task<PresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string folder, TimeSpan expiration);

    /// <summary>
    /// Xóa file t? cloud storage
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Copy file trong cloud storage
    /// </summary>
    Task<string> CopyFileAsync(string sourceUrl, string destinationPath);

    /// <summary>
    /// Ki?m tra file có t?n t?i không
    /// </summary>
    Task<bool> FileExistsAsync(string fileUrl);

    /// <summary>
    /// L?y metadata c?a file
    /// </summary>
    Task<CloudFileMetadata> GetFileMetadataAsync(string fileUrl);
}

/// <summary>
/// Metadata c?a file trong cloud storage
/// </summary>
public class CloudFileMetadata
{
    public string Url { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string ETag { get; set; } = string.Empty;
    public Dictionary<string, string> CustomMetadata { get; set; } = new();
}

/// <summary>
/// Azure Blob Storage implementation
/// </summary>
public interface IAzureBlobStorageProvider : ICloudStorageProvider
{
    Task<string> GetSharedAccessSignatureAsync(string fileName, TimeSpan expiration);
}

/// <summary>
/// AWS S3 implementation
/// </summary>
public interface IAwsS3StorageProvider : ICloudStorageProvider
{
    Task<string> GetCloudFrontUrlAsync(string s3Url);
}

/// <summary>
/// Google Cloud Storage implementation
/// </summary>
public interface IGoogleCloudStorageProvider : ICloudStorageProvider
{
    Task<string> GetSignedUrlAsync(string fileName, TimeSpan expiration);
}