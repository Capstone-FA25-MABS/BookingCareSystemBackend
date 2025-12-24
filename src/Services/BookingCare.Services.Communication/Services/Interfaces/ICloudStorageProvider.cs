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
    /// Tạo presigned URL cho direct upload
    /// </summary>
    Task<PresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string folder, TimeSpan expiration);

    /// <summary>
    /// Xóa file từ cloud storage
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Copy file trong cloud storage
    /// </summary>
    Task<string> CopyFileAsync(string sourceUrl, string destinationPath);

    /// <summary>
    /// Kiểm tra file có tồn tại không
    /// </summary>
    Task<bool> FileExistsAsync(string fileUrl);

    /// <summary>
    /// Lấy metadata của file
    /// </summary>
    Task<CloudFileMetadata> GetFileMetadataAsync(string fileUrl);
}

/// <summary>
/// Metadata của file trong cloud storage
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
