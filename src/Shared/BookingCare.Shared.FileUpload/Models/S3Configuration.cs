using System.ComponentModel.DataAnnotations;

namespace BookingCare.Shared.FileUpload.Models;

public class S3Configuration
{
    public const string SectionName = "AWS:S3";
    
    [Required]
    public string AccessKey { get; set; } = string.Empty;
    
    [Required]
    public string SecretKey { get; set; } = string.Empty;
    
    [Required]
    public string BucketName { get; set; } = string.Empty;
    
    [Required]
    public string Region { get; set; } = "us-east-1";
    
    [Required]
    public string CloudFrontDomain { get; set; } = string.Empty;
    
    public string? CloudFrontDistributionId { get; set; }
    
    public int PresignedUrlExpiryHours { get; set; } = 1;
    
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    
    public string[] AllowedFileExtensions { get; set; } = 
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt"
    };
    
    public string FileUploadPath { get; set; } = "uploads/";
}

public class CloudFrontConfiguration
{
    public const string SectionName = "AWS:CloudFront";
    
    public string? DistributionId { get; set; }
    
    public string Domain { get; set; } = string.Empty;
    
    public int CacheTtlSeconds { get; set; } = 86400; // 1 day
    
    public bool EnableInvalidation { get; set; } = true;
}