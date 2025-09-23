# BookingCare.Shared.FileUpload

A comprehensive file upload service for AWS S3 with CloudFront CDN integration, designed for the BookingCare medical appointment booking system.

## Features

- ✅ Single and multiple file uploads to AWS S3
- ✅ CloudFront CDN integration for fast content delivery
- ✅ Presigned URLs for direct client uploads
- ✅ File validation (size, extension, content type)
- ✅ CloudFront cache invalidation
- ✅ Automatic unique filename generation
- ✅ Comprehensive error handling and logging
- ✅ RESTful API endpoints
- ✅ Support for metadata and custom folders
- ✅ File existence checking and info retrieval

## Installation

1. Add the project reference to your service:

```xml
<ProjectReference Include="..\..\Shared\BookingCare.Shared.FileUpload\BookingCare.Shared.FileUpload.csproj" />
```

2. Install required NuGet packages (already included in the shared project):
   - AWSSDK.S3
   - AWSSDK.CloudFront
   - Microsoft.Extensions.Configuration.Abstractions
   - Microsoft.Extensions.DependencyInjection.Abstractions

## Configuration

### appsettings.json

```json
{
  "AWS": {
    "S3": {
      "AccessKey": "your-aws-access-key",
      "SecretKey": "your-aws-secret-key",
      "BucketName": "your-s3-bucket-name",
      "Region": "us-east-1",
      "CloudFrontDomain": "your-cloudfront-domain.cloudfront.net",
      "CloudFrontDistributionId": "your-distribution-id",
      "PresignedUrlExpiryHours": 1,
      "MaxFileSizeBytes": 10485760,
      "AllowedFileExtensions": [".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt"],
      "FileUploadPath": "uploads/"
    },
    "CloudFront": {
      "DistributionId": "your-distribution-id",
      "Domain": "your-cloudfront-domain.cloudfront.net",
      "CacheTtlSeconds": 86400,
      "EnableInvalidation": true
    }
  }
}
```

### Environment Variables (Production)

```bash
AWS__S3__AccessKey=your-access-key
AWS__S3__SecretKey=your-secret-key
AWS__S3__BucketName=your-bucket-name
AWS__S3__Region=us-east-1
AWS__S3__CloudFrontDomain=your-domain.cloudfront.net
AWS__CloudFront__DistributionId=your-distribution-id
```

## Setup

### Program.cs / Startup.cs

```csharp
using BookingCare.Shared.FileUpload.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add S3 File Upload services
builder.Services.AddS3FileUpload(builder.Configuration);

// Or with custom configuration
builder.Services.AddS3FileUpload(s3Config =>
{
    s3Config.BucketName = "my-bucket";
    s3Config.Region = "us-east-1";
    // ... other settings
}, cloudFrontConfig =>
{
    cloudFrontConfig.Domain = "my-domain.cloudfront.net";
    // ... other settings
});

var app = builder.Build();

// Map controllers
app.MapControllers();

app.Run();
```

## Usage

### 1. Dependency Injection

```csharp
public class MyController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;

    public MyController(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }
}
```

### 2. Upload Single File

```csharp
[HttpPost("upload-avatar")]
public async Task<IActionResult> UploadAvatar(IFormFile file)
{
    using var stream = file.OpenReadStream();
    
    var request = new FileUploadRequest
    {
        FileStream = stream,
        FileName = file.FileName,
        ContentType = file.ContentType,
        Folder = "avatars",
        GenerateUniqueFileName = true,
        Metadata = new Dictionary<string, string>
        {
            ["UserId"] = "123",
            ["UploadType"] = "Avatar"
        }
    };

    var result = await _fileUploadService.UploadFileAsync(request);
    
    if (result.Success)
    {
        return Ok(new 
        { 
            CloudFrontUrl = result.CloudFrontUrl,
            S3Key = result.S3Key 
        });
    }

    return BadRequest(result.ErrorMessage);
}
```

### 3. Upload Multiple Files

```csharp
[HttpPost("upload-documents")]
public async Task<IActionResult> UploadDocuments(IFormFileCollection files)
{
    var requests = files.Select(file => new FileUploadRequest
    {
        FileStream = file.OpenReadStream(),
        FileName = file.FileName,
        ContentType = file.ContentType,
        Folder = "documents",
        GenerateUniqueFileName = true
    });

    var result = await _fileUploadService.UploadMultipleFilesAsync(requests);
    
    return Ok(new
    {
        SuccessCount = result.SuccessCount,
        FailureCount = result.FailureCount,
        Files = result.Results.Where(r => r.Success).Select(r => new
        {
            CloudFrontUrl = r.CloudFrontUrl,
            S3Key = r.S3Key,
            FileName = r.FileName
        })
    });
}
```

### 4. Generate Presigned Upload URL

```csharp
[HttpPost("presigned-url")]
public async Task<IActionResult> GetPresignedUploadUrl([FromBody] PresignedUrlRequest request)
{
    var result = await _fileUploadService.GeneratePresignedUploadUrlAsync(request);
    
    if (result.Success)
    {
        return Ok(new
        {
            UploadUrl = result.UploadUrl,
            CloudFrontUrl = result.CloudFrontUrl,
            ExpiresAt = result.ExpiresAt
        });
    }

    return BadRequest(result.ErrorMessage);
}
```

### 5. Delete Files

```csharp
[HttpDelete("files/{*s3Key}")]
public async Task<IActionResult> DeleteFile(string s3Key)
{
    var success = await _fileUploadService.DeleteFileAsync(s3Key);
    return Ok(success);
}

[HttpDelete("files/bulk")]
public async Task<IActionResult> DeleteMultipleFiles([FromBody] string[] s3Keys)
{
    var results = await _fileUploadService.DeleteMultipleFilesAsync(s3Keys);
    return Ok(results);
}
```

## API Endpoints

The service provides RESTful API endpoints:

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/fileupload/upload` | Upload single file |
| POST | `/api/fileupload/upload-multiple` | Upload multiple files |
| POST | `/api/fileupload/presigned-upload-url` | Generate presigned upload URL |
| GET | `/api/fileupload/presigned-download-url/{s3Key}` | Generate presigned download URL |
| DELETE | `/api/fileupload/{s3Key}` | Delete file |
| DELETE | `/api/fileupload/multiple` | Delete multiple files |
| HEAD | `/api/fileupload/{s3Key}` | Check if file exists |
| GET | `/api/fileupload/info/{s3Key}` | Get file information |
| POST | `/api/fileupload/invalidate-cache` | Invalidate CloudFront cache |
| GET | `/api/fileupload/cloudfront-url` | Get CloudFront URL |

## Best Practices

### 1. Medical Images
```csharp
var request = new FileUploadRequest
{
    FileStream = imageStream,
    FileName = "patient_scan.jpg",
    ContentType = "image/jpeg",
    Folder = "medical-images",
    Metadata = new Dictionary<string, string>
    {
        ["PatientId"] = patientId,
        ["ScanType"] = "X-Ray",
        ["UploadedBy"] = doctorId
    }
};
```

### 2. Document Storage
```csharp
var request = new FileUploadRequest
{
    FileStream = documentStream,
    FileName = "medical_report.pdf",
    ContentType = "application/pdf",
    Folder = "reports",
    GenerateUniqueFileName = true
};
```

### 3. Avatar/Profile Images
```csharp
var request = new FileUploadRequest
{
    FileStream = avatarStream,
    FileName = "profile.jpg",
    ContentType = "image/jpeg",
    Folder = "avatars",
    GenerateUniqueFileName = true
};
```

## Security Considerations

1. **File Validation**: Always validate file types and sizes
2. **Access Control**: Use appropriate S3 bucket policies
3. **Presigned URLs**: Set appropriate expiry times
4. **Metadata**: Don't include sensitive information in metadata
5. **CloudFront**: Configure proper caching headers

## Monitoring

The service provides comprehensive logging for:
- File upload success/failure
- S3 operations
- CloudFront cache invalidations
- Validation errors

## Error Handling

All methods return detailed error information:
- Validation errors
- AWS service errors
- Network timeout errors
- Permission errors

## Performance Considerations

1. **Parallel Uploads**: Multiple files are uploaded concurrently
2. **CloudFront Caching**: Reduces S3 requests and improves performance
3. **Presigned URLs**: Allows direct client uploads, reducing server load
4. **Stream Processing**: Files are streamed, not loaded into memory

## License

This is part of the BookingCare medical appointment booking system.