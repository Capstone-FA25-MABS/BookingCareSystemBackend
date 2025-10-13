namespace BookingCare.Shared.FileUpload.Helpers;

/// <summary>
/// Helper class for common file upload operations
/// </summary>
public static class FileUploadHelper
{
    /// <summary>
    /// Extract S3 key from full URL
    /// </summary>
    /// <param name="url">The full URL</param>
    /// <param name="expectedFolder">Expected folder prefix (e.g., "avatars", "appointments")</param>
    /// <returns>S3 key or null if extraction fails</returns>
    public static string? ExtractS3KeyFromUrl(string url, string? expectedFolder = null)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath.TrimStart('/');

            // If URL contains bucket name in path, remove it
            var segments = path.Split('/');

            // Check if first segment is not the expected folder
            if (segments.Length > 2 && !string.IsNullOrEmpty(expectedFolder) && segments[0] != expectedFolder)
            {
                // Assuming first segment is bucket name, remove it
                path = string.Join("/", segments.Skip(1));
            }

            return path;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Generates a unique file name with timestamp
    /// </summary>
    /// <param name="originalFileName">Original file name</param>
    /// <returns>Unique file name</returns>
    public static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N")[..8]; // First 8 characters of GUID

        return $"{nameWithoutExtension}_{timestamp}_{uniqueId}{extension}";
    }

    /// <summary>
    /// Gets file extension from file name
    /// </summary>
    /// <param name="fileName">The file name</param>
    /// <returns>File extension in lowercase</returns>
    public static string GetFileExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    /// <summary>
    /// Validates if URL is a valid S3/CloudFront URL
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValidStorageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        try
        {
            var uri = new Uri(url);
            // Check if URL is HTTPS and has a valid host
            return uri.Scheme == "https" && !string.IsNullOrWhiteSpace(uri.Host);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Creates file path for storage (folder/filename)
    /// </summary>
    /// <param name="folder">The folder name</param>
    /// <param name="fileName">The file name</param>
    /// <returns>Combined path</returns>
    public static string CreateStoragePath(string folder, string fileName)
    {
        return $"{folder.TrimEnd('/')}/{fileName}";
    }
}

