using BookingCare.Services.Communication.Enums;

namespace BookingCare.Services.Communication.Services.Interfaces;

/// <summary>
/// Interface cho File Upload service
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Upload file lên cloud storage và trả về URL
    /// </summary>
    Task<FileUploadResult> UploadFileAsync(IFormFile file, string userId, MessageType messageType);

    /// <summary>
    /// Upload nhiều files cùng lúc
    /// </summary>
    Task<List<FileUploadResult>> UploadMultipleFilesAsync(IEnumerable<IFormFile> files, string userId, MessageType messageType);

    /// <summary>
    /// Tạo presigned URL để client upload trực tiếp lên cloud
    /// </summary>
    Task<PresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string userId, MessageType messageType);

    /// <summary>
    /// Xóa file từ cloud storage
    /// </summary>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Tạo thumbnail cho images/videos
    /// </summary>
    Task<string?> GenerateThumbnailAsync(string originalUrl);

    /// <summary>
    /// Validate file type và size
    /// </summary>
    Task<FileValidationResult> ValidateFileAsync(IFormFile file, MessageType messageType);
}

/// <summary>
/// Result của file upload
/// </summary>
public class FileUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; } // For video/audio
}

/// <summary>
/// Result của presigned URL
/// </summary>
public class PresignedUrlResult
{
    public string UploadUrl { get; set; } = string.Empty;
    public string FinalUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Result của file validation
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? DetectedMimeType { get; set; }
}