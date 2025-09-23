namespace BookingCare.Shared.FileUpload.Models;

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FileUrl { get; set; }
    public string? CloudFrontUrl { get; set; }
    public string? FileName { get; set; }
    public string? S3Key { get; set; }
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class MultipleFileUploadResult
{
    public List<FileUploadResult> Results { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
    public bool AllSuccessful => FailureCount == 0;
}

public class FileUploadRequest
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? Folder { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool GenerateUniqueFileName { get; set; } = true;
}

public class PresignedUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? Folder { get; set; }
    public int ExpiryHours { get; set; } = 1;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class PresignedUrlResult
{
    public bool Success { get; set; }
    public string? UploadUrl { get; set; }
    public string? S3Key { get; set; }
    public string? FinalUrl { get; set; }
    public string? CloudFrontUrl { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}