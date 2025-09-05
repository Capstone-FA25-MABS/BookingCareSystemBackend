using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using BookingCare.Services.Communication.Services.Interfaces;
using BookingCare.Services.Communication.Configuration;
using Microsoft.Extensions.Options;

namespace BookingCare.Services.Communication.Services.Implementations;

/// <summary>
/// Cloudinary storage provider implementation
/// </summary>
public class CloudinaryStorageProvider : ICloudStorageProvider
{
    private readonly Cloudinary _cloudinary;
    private readonly FileUploadConfiguration _config;
    private readonly ILogger<CloudinaryStorageProvider> _logger;

    public CloudinaryStorageProvider(
        IOptions<FileUploadConfiguration> config,
        ILogger<CloudinaryStorageProvider> logger)
    {
        _config = config.Value;
        _logger = logger;

        // Initialize Cloudinary
        var account = new Account(
            _config.Cloudinary?.CloudName,
            _config.Cloudinary?.ApiKey,
            _config.Cloudinary?.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    /// <summary>
    /// Upload file lên Cloudinary
    /// </summary>
    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder)
    {
        try
        {
            _logger.LogInformation("Uploading file {FileName} to Cloudinary", fileName);

            // Determine resource type based on content type
            var resourceType = GetResourceType(contentType);

            // Create unique public ID
            var publicId = GeneratePublicId(fileName, folder);

            // Create upload parameters based on resource type
            if (resourceType == ResourceType.Image)
            {
                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(fileName, fileStream),
                    PublicId = publicId,
                    Transformation = GetImageTransformation(),
                    UseFilename = false,
                    UniqueFilename = true,
                    Folder = folder
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
                }

                var fileUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
                _logger.LogInformation("Image uploaded successfully: {Url}", fileUrl);
                return fileUrl ?? "";
            }
            else if (resourceType == ResourceType.Video)
            {
                var uploadParams = new VideoUploadParams()
                {
                    File = new FileDescription(fileName, fileStream),
                    PublicId = publicId,
                    UseFilename = false,
                    UniqueFilename = true,
                    Folder = folder
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
                }

                var fileUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
                _logger.LogInformation("Video uploaded successfully: {Url}", fileUrl);
                return fileUrl ?? "";
            }
            else
            {
                var uploadParams = new RawUploadParams()
                {
                    File = new FileDescription(fileName, fileStream),
                    PublicId = publicId,
                    UseFilename = false,
                    UniqueFilename = true,
                    Folder = folder
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                if (uploadResult.Error != null)
                {
                    throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
                }

                var fileUrl = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
                _logger.LogInformation("Raw file uploaded successfully: {Url}", fileUrl);
                return fileUrl ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to Cloudinary", fileName);
            throw;
        }
    }

    /// <summary>
    /// Generate presigned URL cho Cloudinary (sử dụng signed URLs)
    /// </summary>
    public async Task<PresignedUrlResult> GeneratePresignedUrlAsync(string fileName, string contentType, string folder, TimeSpan expiration)
    {
        await Task.CompletedTask;

        try
        {
            _logger.LogInformation("Generating signed URL for {FileName}", fileName);

            var publicId = GeneratePublicId(fileName, folder);
            var resourceType = GetResourceType(contentType);

            // Create signed URL parameters
            var parameters = new SortedDictionary<string, object>
            {
                { "public_id", publicId },
                { "resource_type", resourceType.ToString().ToLower() },
                { "timestamp", DateTimeOffset.UtcNow.Add(expiration).ToUnixTimeSeconds() }
            };

            // Generate signature
            var signature = _cloudinary.Api.SignParameters(parameters);

            var uploadUrl = $"https://api.cloudinary.com/v1_1/{_config.Cloudinary?.CloudName}/{resourceType.ToString().ToLower()}/upload";
            var finalUrl = $"https://res.cloudinary.com/{_config.Cloudinary?.CloudName}/{resourceType.ToString().ToLower()}/upload/{publicId}";

            return new PresignedUrlResult
            {
                UploadUrl = uploadUrl,
                FinalUrl = finalUrl,
                ExpiresAt = DateTime.UtcNow.Add(expiration),
                Headers = new Dictionary<string, string>
                {
                    ["api_key"] = _config.Cloudinary?.ApiKey ?? "",
                    ["timestamp"] = parameters["timestamp"].ToString() ?? "",
                    ["signature"] = signature,
                    ["public_id"] = publicId
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating signed URL for {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Xóa file từ Cloudinary
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            _logger.LogInformation("Deleting file from Cloudinary: {FileUrl}", fileUrl);

            // Extract public_id from URL
            var publicId = ExtractPublicIdFromUrl(fileUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("Could not extract public_id from URL: {FileUrl}", fileUrl);
                return false;
            }

            // Delete from Cloudinary
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            var success = result.Result == "ok";

            if (success)
            {
                _logger.LogInformation("File deleted successfully: {PublicId}", publicId);
            }
            else
            {
                _logger.LogWarning("Failed to delete file: {PublicId}, Result: {Result}", publicId, result.Result);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from Cloudinary: {FileUrl}", fileUrl);
            return false;
        }
    }

    /// <summary>
    /// Copy file trong Cloudinary
    /// </summary>
    public async Task<string> CopyFileAsync(string sourceUrl, string destinationPath)
    {
        try
        {
            // Extract source public_id
            var sourcePublicId = ExtractPublicIdFromUrl(sourceUrl);
            if (string.IsNullOrEmpty(sourcePublicId))
            {
                throw new ArgumentException($"Invalid source URL: {sourceUrl}");
            }

            // Use Cloudinary's upload from URL feature
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(sourceUrl),
                PublicId = destinationPath,
                Type = "upload"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
            {
                throw new Exception($"Cloudinary copy failed: {result.Error.Message}");
            }

            return result.SecureUrl?.ToString() ?? result.Url?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file in Cloudinary from {SourceUrl} to {DestinationPath}", sourceUrl, destinationPath);
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra file có tồn tại không
    /// </summary>
    public async Task<bool> FileExistsAsync(string fileUrl)
    {
        try
        {
            var publicId = ExtractPublicIdFromUrl(fileUrl);
            if (string.IsNullOrEmpty(publicId))
                return false;

            var result = await _cloudinary.GetResourceAsync(publicId);
            return result.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Lấy metadata của file
    /// </summary>
    public async Task<CloudFileMetadata> GetFileMetadataAsync(string fileUrl)
    {
        try
        {
            var publicId = ExtractPublicIdFromUrl(fileUrl);
            if (string.IsNullOrEmpty(publicId))
            {
                throw new ArgumentException($"Invalid Cloudinary URL: {fileUrl}");
            }

            var result = await _cloudinary.GetResourceAsync(publicId);

            return new CloudFileMetadata
            {
                Url = fileUrl,
                Size = result.Bytes,
                ContentType = result.Format ?? "unknown",
                LastModified = DateTime.TryParse(result.CreatedAt, out var createdAt) ? createdAt : DateTime.UtcNow,
                ETag = result.Version?.ToString() ?? "",
                CustomMetadata = result.Metadata?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? "") ?? new()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata from Cloudinary: {FileUrl}", fileUrl);
            throw;
        }
    }

    #region Private Helper Methods

    private ResourceType GetResourceType(string contentType)
    {
        return contentType switch
        {
            var ct when ct.StartsWith("image/") => ResourceType.Image,
            var ct when ct.StartsWith("video/") => ResourceType.Video,
            var ct when ct.StartsWith("audio/") => ResourceType.Video, // Audio is handled as video in Cloudinary
            _ => ResourceType.Raw
        };
    }

    private string GeneratePublicId(string fileName, string folder)
    {
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
        var sanitizedName = System.Text.RegularExpressions.Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9_-]", "_");
        var uniqueId = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}_{sanitizedName}";

        return string.IsNullOrEmpty(folder) ? uniqueId : $"{folder}/{uniqueId}";
    }

    private Transformation? GetImageTransformation()
    {
        if (!_config.Cloudinary?.AutoOptimize == true) return null;

        return new Transformation()
            .Quality("auto:good")
            .FetchFormat("auto");
    }

    private string? ExtractPublicIdFromUrl(string url)
    {
        try
        {
            // Cloudinary URL format: https://res.cloudinary.com/{cloud_name}/{resource_type}/upload/v{version}/{public_id}.{format}
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Find the upload segment and extract public_id
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i] == "upload" && i + 1 < segments.Length)
                {
                    var publicIdWithFormat = string.Join("/", segments.Skip(i + 1));

                    // Skip version if present (v{number})
                    if (publicIdWithFormat.StartsWith("v") && publicIdWithFormat.Length > 1 && char.IsDigit(publicIdWithFormat[1]))
                    {
                        var versionEnd = publicIdWithFormat.IndexOf('/');
                        if (versionEnd > 0)
                        {
                            publicIdWithFormat = publicIdWithFormat.Substring(versionEnd + 1);
                        }
                    }

                    // Remove file extension
                    var lastDotIndex = publicIdWithFormat.LastIndexOf('.');
                    return lastDotIndex > 0 ? publicIdWithFormat.Substring(0, lastDotIndex) : publicIdWithFormat;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    #endregion
}