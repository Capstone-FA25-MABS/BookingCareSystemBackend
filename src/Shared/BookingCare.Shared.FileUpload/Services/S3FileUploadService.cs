using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.S3;
using Amazon.S3.Model;
using BookingCare.Shared.FileUpload.Helpers;
using BookingCare.Shared.FileUpload.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace BookingCare.Shared.FileUpload.Services;

public class S3FileUploadService : IFileUploadService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonCloudFront? _cloudFrontClient;
    private readonly S3Configuration _s3Config;
    private readonly CloudFrontConfiguration _cloudFrontConfig;
    private readonly ILogger<S3FileUploadService> _logger;

    public S3FileUploadService(
        IAmazonS3 s3Client,
        IAmazonCloudFront? cloudFrontClient,
        IOptions<S3Configuration> s3Options,
        IOptions<CloudFrontConfiguration> cloudFrontOptions,
        ILogger<S3FileUploadService> logger)
    {
        _s3Client = s3Client;
        _cloudFrontClient = cloudFrontClient;
        _s3Config = s3Options.Value;
        _cloudFrontConfig = cloudFrontOptions.Value;
        _logger = logger;
    }

    public async Task<FileUploadResult> UploadFileAsync(FileUploadRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            var (isValid, errorMessage) = ValidateFile(request.FileName, request.FileStream.Length, request.ContentType);
            if (!isValid)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            // Generate S3 key
            var s3Key = GenerateS3Key(request.FileName, request.Folder, request.GenerateUniqueFileName);

            // Save stream length BEFORE upload (AWS SDK may dispose/close the stream)
            var fileSize = request.FileStream.Length;

            // Determine proper Content-Type with charset for text files
            var contentTypeWithCharset = GetContentTypeWithCharset(request.ContentType, request.FileName);

            // Create S3 request
            var s3Request = new PutObjectRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key,
                InputStream = request.FileStream,
                ContentType = contentTypeWithCharset,  // Include charset
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                CannedACL = S3CannedACL.Private
            };

            // Add Content-Encoding header for text files
            if (IsTextBasedFile(request.ContentType, request.FileName))
            {
                s3Request.Headers.ContentEncoding = "utf-8";
            }

            // Add metadata
            foreach (var metadata in request.Metadata)
            {
                s3Request.Metadata.Add(metadata.Key, metadata.Value);
            }

            // Add encoding metadata
            s3Request.Metadata.Add("x-amz-meta-encoding", "utf-8");
            s3Request.Metadata.Add("x-amz-meta-original-filename", request.FileName);

            // Upload to S3
            var response = await _s3Client.PutObjectAsync(s3Request, cancellationToken);

            if (response.HttpStatusCode == HttpStatusCode.OK)
            {
                var fileUrl = $"https://{_s3Config.BucketName}.s3.{_s3Config.Region}.amazonaws.com/{s3Key}";
                var cloudFrontUrl = GetCloudFrontUrl(s3Key);

                _logger.LogInformation("File uploaded successfully to S3. Key: {S3Key}", s3Key);

                return new FileUploadResult
                {
                    Success = true,
                    FileUrl = fileUrl,
                    CloudFrontUrl = cloudFrontUrl,
                    FileName = request.FileName,
                    S3Key = s3Key,
                    FileSize = fileSize, // Use saved value instead of accessing disposed stream
                    ContentType = contentTypeWithCharset
                };
            }

            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = "Failed to upload file to S3"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3: {FileName}", request.FileName);
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Get Content-Type with proper charset for text-based files
    /// </summary>
    private static string GetContentTypeWithCharset(string contentType, string fileName)
    {
        // If already has charset, return as-is
        if (contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        // Add UTF-8 charset for text-based files
        if (IsTextBasedFile(contentType, fileName))
        {
            return $"{contentType}; charset=utf-8";
        }

        return contentType;
    }

    /// <summary>
    /// Check if file is text-based and needs UTF-8 encoding
    /// </summary>
    private static bool IsTextBasedFile(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        // Check by Content-Type
        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check common text-based content types
        var textBasedContentTypes = new[]
        {
            "application/json",
            "application/xml",
            "application/javascript",
            "application/x-javascript",
            "application/ecmascript",
            "application/xhtml+xml",
            "application/rss+xml",
            "application/atom+xml",
            "application/x-httpd-php",
            "application/x-sh",
            "application/x-csh"
        };

        if (textBasedContentTypes.Any(t => contentType.Equals(t, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check by file extension
        var textExtensions = new[]
        {
            ".txt", ".html", ".htm", ".css", ".js", ".json", ".xml",
            ".csv", ".md", ".log", ".sql", ".sh", ".bat", ".ps1",
            ".php", ".py", ".rb", ".java", ".cs", ".cpp", ".h",
            ".yaml", ".yml", ".toml", ".ini", ".conf", ".config"
        };

        return textExtensions.Contains(extension);
    }

    public async Task<MultipleFileUploadResult> UploadMultipleFilesAsync(IEnumerable<FileUploadRequest> requests, CancellationToken cancellationToken = default)
    {
        var result = new MultipleFileUploadResult();
        var tasks = new List<Task<FileUploadResult>>();

        foreach (var request in requests)
        {
            tasks.Add(UploadFileAsync(request, cancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        foreach (var uploadResult in results)
        {
            result.Results.Add(uploadResult);

            if (uploadResult.Success)
            {
                result.SuccessCount++;
            }
            else
            {
                result.FailureCount++;
                if (!string.IsNullOrEmpty(uploadResult.ErrorMessage))
                {
                    result.ErrorMessages.Add(uploadResult.ErrorMessage);
                }
            }
        }

        return result;
    }

    public async Task<PresignedUrlResult> GeneratePresignedUploadUrlAsync(PresignedUrlRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate file
            var (isValid, errorMessage) = ValidateFile(request.FileName, _s3Config.MaxFileSizeBytes, request.ContentType);
            if (!isValid)
            {
                return new PresignedUrlResult
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            var s3Key = GenerateS3Key(request.FileName, request.Folder, true);
            var expiresAt = DateTime.UtcNow.AddHours(request.ExpiryHours);

            var presignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key,
                Verb = HttpVerb.PUT,
                Expires = expiresAt,
                ContentType = request.ContentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            // Add metadata
            foreach (var metadata in request.Metadata)
            {
                presignedRequest.Metadata.Add(metadata.Key, metadata.Value);
            }

            var uploadUrl = await _s3Client.GetPreSignedURLAsync(presignedRequest);
            var finalUrl = $"https://{_s3Config.BucketName}.s3.{_s3Config.Region}.amazonaws.com/{s3Key}";
            var cloudFrontUrl = GetCloudFrontUrl(s3Key);

            return new PresignedUrlResult
            {
                Success = true,
                UploadUrl = uploadUrl,
                S3Key = s3Key,
                FinalUrl = finalUrl,
                CloudFrontUrl = cloudFrontUrl,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned upload URL for file: {FileName}", request.FileName);
            return new PresignedUrlResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PresignedUrlResult> GeneratePresignedDownloadUrlAsync(string s3Key, int expiryHours = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

            var presignedRequest = new GetPreSignedUrlRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key,
                Verb = HttpVerb.GET,
                Expires = expiresAt
            };

            var downloadUrl = await _s3Client.GetPreSignedURLAsync(presignedRequest);

            return new PresignedUrlResult
            {
                Success = true,
                UploadUrl = downloadUrl,
                S3Key = s3Key,
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned download URL for S3 key: {S3Key}", s3Key);
            return new PresignedUrlResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> DeleteFileAsync(string s3Key, CancellationToken cancellationToken = default)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

            if (response.HttpStatusCode == HttpStatusCode.NoContent)
            {
                _logger.LogInformation("File deleted successfully from S3. Key: {S3Key}", s3Key);

                // Invalidate CloudFront cache if enabled
                if (_cloudFrontConfig.EnableInvalidation && !string.IsNullOrEmpty(_cloudFrontConfig.DistributionId))
                {
                    await InvalidateCloudFrontCacheAsync(new[] { s3Key }, cancellationToken);
                }

                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3. Key: {S3Key}", s3Key);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> DeleteMultipleFilesAsync(IEnumerable<string> s3Keys, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();
        var tasks = new List<Task<(string Key, bool Success)>>();

        foreach (var s3Key in s3Keys)
        {
            tasks.Add(DeleteFileAndReturnResultAsync(s3Key, cancellationToken));
        }

        var deleteResults = await Task.WhenAll(tasks);

        foreach (var (key, success) in deleteResults)
        {
            results[key] = success;
        }

        return results;
    }

    private async Task<(string Key, bool Success)> DeleteFileAndReturnResultAsync(string s3Key, CancellationToken cancellationToken)
    {
        var success = await DeleteFileAsync(s3Key, cancellationToken);
        return (s3Key, success);
    }

    public async Task<bool> FileExistsAsync(string s3Key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key
            };

            await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists in S3. Key: {S3Key}", s3Key);
            return false;
        }
    }

    public async Task<FileUploadResult?> GetFileInfoAsync(string s3Key, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _s3Config.BucketName,
                Key = s3Key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request, cancellationToken);
            var fileUrl = $"https://{_s3Config.BucketName}.s3.{_s3Config.Region}.amazonaws.com/{s3Key}";
            var cloudFrontUrl = GetCloudFrontUrl(s3Key);

            return new FileUploadResult
            {
                Success = true,
                FileUrl = fileUrl,
                CloudFrontUrl = cloudFrontUrl,
                S3Key = s3Key,
                FileSize = response.ContentLength,
                ContentType = response.Headers.ContentType,
                UploadedAt = response.LastModified
            };
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info from S3. Key: {S3Key}", s3Key);
            return null;
        }
    }

    public async Task<bool> InvalidateCloudFrontCacheAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        if (_cloudFrontClient == null || string.IsNullOrEmpty(_cloudFrontConfig.DistributionId))
        {
            _logger.LogWarning("CloudFront client or distribution ID not configured");
            return false;
        }

        try
        {
            var pathsList = paths.Select(p => p.StartsWith('/') ? p : $"/{p}").ToList();

            var invalidationRequest = new CreateInvalidationRequest
            {
                DistributionId = _cloudFrontConfig.DistributionId,
                InvalidationBatch = new InvalidationBatch
                {
                    CallerReference = Guid.NewGuid().ToString(),
                    Paths = new Paths
                    {
                        Quantity = pathsList.Count,
                        Items = pathsList
                    }
                }
            };

            var response = await _cloudFrontClient.CreateInvalidationAsync(invalidationRequest, cancellationToken);

            _logger.LogInformation("CloudFront cache invalidation created. Invalidation ID: {InvalidationId}",
                response.Invalidation.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating CloudFront cache for paths: {Paths}", string.Join(", ", paths));
            return false;
        }
    }

    public string GetCloudFrontUrl(string s3Key)
    {
        // Use CloudFrontDomain from S3 config if available, otherwise use CloudFront config Domain
        var cloudFrontDomain = !string.IsNullOrEmpty(_s3Config.CloudFrontDomain)
            ? _s3Config.CloudFrontDomain
            : _cloudFrontConfig.Domain;

        if (string.IsNullOrEmpty(cloudFrontDomain))
        {
            return $"https://{_s3Config.BucketName}.s3.{_s3Config.Region}.amazonaws.com/{s3Key}";
        }

        return $"https://{cloudFrontDomain}/{s3Key}";
    }

    public (bool IsValid, string? ErrorMessage) ValidateFile(string fileName, long fileSize, string contentType)
    {
        // Check file size
        if (fileSize > _s3Config.MaxFileSizeBytes)
        {
            return (false, $"File size exceeds maximum allowed size of {_s3Config.MaxFileSizeBytes / (1024 * 1024)} MB");
        }

        // Check file extension
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_s3Config.AllowedFileExtensions.Contains(extension))
        {
            return (false, $"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", _s3Config.AllowedFileExtensions)}");
        }

        // Additional content type validation can be added here

        return (true, null);
    }

    private string GenerateS3Key(string fileName, string? folder, bool generateUnique)
    {
        var cleanFolder = !string.IsNullOrEmpty(folder) ? folder.Trim('/') + "/" : _s3Config.FileUploadPath;

        if (generateUnique)
        {
            // ✅ FIX: Use helper to generate URL-safe unique filename
            fileName = FileNameHelper.GenerateUniqueFileName(fileName);
        }
        else
        {
            // ✅ FIX: Sanitize filename even when not generating unique
            fileName = FileNameHelper.SanitizeFileName(fileName);
        }

        return $"{cleanFolder}{fileName}";
    }
}