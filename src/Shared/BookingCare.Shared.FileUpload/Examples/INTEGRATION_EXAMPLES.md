# S3 File Upload Integration Examples

## Integration with User Service

### 1. Add Project Reference

Add to `BookingCare.Services.User.csproj`:

```xml
<ProjectReference Include="..\..\Shared\BookingCare.Shared.FileUpload\BookingCare.Shared.FileUpload.csproj" />
```

### 2. Configure in Program.cs

```csharp
using BookingCare.Shared.FileUpload.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add S3 File Upload services
builder.Services.AddS3FileUpload(builder.Configuration);

// ... other services

var app = builder.Build();
app.Run();
```

### 3. User Profile Picture Upload

```csharp
// UserController.cs
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IUserService _userService;

    public UserController(IFileUploadService fileUploadService, IUserService userService)
    {
        _fileUploadService = fileUploadService;
        _userService = userService;
    }

    [HttpPost("{userId}/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(
        string userId, 
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Validate file
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        // Additional validation for profile pictures
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest("Only JPEG, PNG, and GIF images are allowed for profile pictures");
        }

        try
        {
            using var stream = file.OpenReadStream();
            
            var request = new FileUploadRequest
            {
                FileStream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = $"users/{userId}/profile",
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    ["UserId"] = userId,
                    ["UploadType"] = "ProfilePicture",
                    ["OriginalFileName"] = file.FileName
                }
            };

            var uploadResult = await _fileUploadService.UploadFileAsync(request, cancellationToken);
            
            if (uploadResult.Success)
            {
                // Update user profile with new image URL
                await _userService.UpdateProfilePictureAsync(userId, uploadResult.CloudFrontUrl!);
                
                return Ok(new 
                { 
                    success = true,
                    imageUrl = uploadResult.CloudFrontUrl,
                    s3Key = uploadResult.S3Key,
                    fileName = uploadResult.FileName
                });
            }

            return BadRequest(new { success = false, message = uploadResult.ErrorMessage });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpDelete("{userId}/profile-picture")]
    public async Task<IActionResult> DeleteProfilePicture(string userId)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user?.ProfilePictureS3Key == null)
            {
                return NotFound("No profile picture found");
            }

            var deleteResult = await _fileUploadService.DeleteFileAsync(user.ProfilePictureS3Key);
            
            if (deleteResult)
            {
                await _userService.UpdateProfilePictureAsync(userId, null);
                return Ok(new { success = true, message = "Profile picture deleted successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to delete profile picture" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}
```

## Integration with Doctor Service

### Medical Documents Upload

```csharp
// DoctorController.cs
[ApiController]
[Route("api/[controller]")]
public class DoctorController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IDoctorService _doctorService;

    [HttpPost("{doctorId}/documents")]
    public async Task<IActionResult> UploadMedicalDocuments(
        string doctorId,
        IFormFileCollection files,
        [FromForm] string documentType,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        try
        {
            var requests = files.Select(file => new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = $"doctors/{doctorId}/documents/{documentType}",
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    ["DoctorId"] = doctorId,
                    ["DocumentType"] = documentType,
                    ["UploadDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    ["OriginalFileName"] = file.FileName
                }
            });

            var uploadResult = await _fileUploadService.UploadMultipleFilesAsync(requests, cancellationToken);

            // Save successful uploads to database
            var successfulUploads = uploadResult.Results.Where(r => r.Success).ToList();
            if (successfulUploads.Any())
            {
                await _doctorService.SaveDocumentsAsync(doctorId, successfulUploads, documentType);
            }

            return Ok(new
            {
                success = uploadResult.AllSuccessful,
                successCount = uploadResult.SuccessCount,
                failureCount = uploadResult.FailureCount,
                documents = successfulUploads.Select(r => new
                {
                    fileName = r.FileName,
                    url = r.CloudFrontUrl,
                    s3Key = r.S3Key,
                    uploadedAt = r.UploadedAt
                }),
                errors = uploadResult.ErrorMessages
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}
```

## Integration with Clinic Service

### Clinic Images and Documents

```csharp
// ClinicController.cs
[ApiController]
[Route("api/[controller]")]
public class ClinicController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;
    private readonly IClinicService _clinicService;

    [HttpPost("{clinicId}/images")]
    public async Task<IActionResult> UploadClinicImages(
        string clinicId,
        IFormFileCollection files,
        CancellationToken cancellationToken = default)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("No files provided");
        }

        // Validate image files
        var allowedImageTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        var invalidFiles = files.Where(f => !allowedImageTypes.Contains(f.ContentType)).ToList();
        
        if (invalidFiles.Any())
        {
            return BadRequest($"Invalid file types: {string.Join(", ", invalidFiles.Select(f => f.FileName))}");
        }

        try
        {
            var requests = files.Select(file => new FileUploadRequest
            {
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Folder = $"clinics/{clinicId}/images",
                GenerateUniqueFileName = true,
                Metadata = new Dictionary<string, string>
                {
                    ["ClinicId"] = clinicId,
                    ["ImageType"] = "Gallery",
                    ["UploadDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd")
                }
            });

            var uploadResult = await _fileUploadService.UploadMultipleFilesAsync(requests, cancellationToken);

            // Update clinic with new images
            var successfulUploads = uploadResult.Results.Where(r => r.Success).ToList();
            if (successfulUploads.Any())
            {
                await _clinicService.AddImagesToGalleryAsync(clinicId, successfulUploads);
            }

            return Ok(new
            {
                success = uploadResult.AllSuccessful,
                uploadedCount = uploadResult.SuccessCount,
                images = successfulUploads.Select(r => new
                {
                    url = r.CloudFrontUrl,
                    thumbnailUrl = $"{r.CloudFrontUrl}?w=300&h=200", // If using image processing
                    s3Key = r.S3Key
                })
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}
```

## Presigned URL for Direct Upload

### Frontend Direct Upload Implementation

```csharp
// PresignedUploadController.cs
[ApiController]
[Route("api/presigned-upload")]
public class PresignedUploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;

    [HttpPost("patient-documents")]
    public async Task<IActionResult> GetPresignedUrlForPatientDocument(
        [FromBody] CreatePresignedUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var presignedRequest = new PresignedUrlRequest
            {
                FileName = request.FileName,
                ContentType = request.ContentType,
                Folder = $"patients/{request.PatientId}/documents",
                ExpiryHours = 1,
                Metadata = new Dictionary<string, string>
                {
                    ["PatientId"] = request.PatientId,
                    ["DocumentType"] = request.DocumentType,
                    ["UploadedBy"] = request.UploadedBy
                }
            };

            var result = await _fileUploadService.GeneratePresignedUploadUrlAsync(presignedRequest, cancellationToken);

            if (result.Success)
            {
                return Ok(new
                {
                    uploadUrl = result.UploadUrl,
                    s3Key = result.S3Key,
                    finalUrl = result.CloudFrontUrl,
                    expiresAt = result.ExpiresAt,
                    instructions = new
                    {
                        method = "PUT",
                        headers = new
                        {
                            ContentType = request.ContentType
                        }
                    }
                });
            }

            return BadRequest(result.ErrorMessage);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CreatePresignedUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
}
```

## Background Services

### Cleanup Old Files

```csharp
// FileCleanupBackgroundService.cs
public class FileCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FileCleanupBackgroundService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fileUploadService = scope.ServiceProvider.GetRequiredService<IFileUploadService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                // Get old temporary files to delete
                var oldTempFiles = await userService.GetOldTemporaryFilesAsync(TimeSpan.FromDays(7));
                
                if (oldTempFiles.Any())
                {
                    var deleteResults = await fileUploadService.DeleteMultipleFilesAsync(oldTempFiles);
                    var deletedCount = deleteResults.Values.Count(success => success);
                    
                    _logger.LogInformation("Cleaned up {DeletedCount} old temporary files", deletedCount);
                }

                // Wait 24 hours before next cleanup
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file cleanup");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry in 1 hour
            }
        }
    }
}
```

## Configuration Examples

### Development Environment

```json
{
  "AWS": {
    "S3": {
      "AccessKey": "AKIA...",
      "SecretKey": "your-secret-key",
      "BucketName": "bookingcare-dev-files",
      "Region": "us-east-1",
      "CloudFrontDomain": "d123456789.cloudfront.net",
      "MaxFileSizeBytes": 52428800,
      "AllowedFileExtensions": [".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".txt", ".mp4", ".mov"],
      "FileUploadPath": "dev-uploads/"
    },
    "CloudFront": {
      "DistributionId": "E123456789",
      "Domain": "d123456789.cloudfront.net",
      "EnableInvalidation": true
    }
  }
}
```

### Production Environment

```json
{
  "AWS": {
    "S3": {
      "BucketName": "bookingcare-prod-files",
      "Region": "us-east-1",
      "CloudFrontDomain": "files.bookingcare.com",
      "MaxFileSizeBytes": 104857600,
      "FileUploadPath": "uploads/"
    },
    "CloudFront": {
      "Domain": "files.bookingcare.com",
      "EnableInvalidation": true,
      "CacheTtlSeconds": 2592000
    }
  }
}
```

This integration provides a complete file upload solution for your medical booking system with proper organization, security, and performance optimization.