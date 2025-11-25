using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper class to validate audio file uploads with consistent validation logic
/// </summary>
public static class AudioFileValidator
{
    private static readonly string[] DefaultAllowedMimeTypes = { "audio/webm", "audio/wav" };
    private static readonly string[] DefaultAllowedExtensions = { ".webm", ".wav" };

    /// <summary>
    /// Validation result with success status and error message
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public string? LogMessage { get; set; }

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Fail(string errorMessage, string? logMessage = null) =>
            new()
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                LogMessage = logMessage,
            };
    }

    /// <summary>
    /// Validates an audio file for upload
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <param name="maxFileSizeBytes">Maximum allowed file size in bytes</param>
    /// <param name="allowedMimeTypes">Optional custom allowed MIME types</param>
    /// <param name="allowedExtensions">Optional custom allowed extensions</param>
    /// <returns>Validation result</returns>
    public static ValidationResult ValidateAudioFile(
        IFormFile? file,
        long maxFileSizeBytes,
        string[]? allowedMimeTypes = null,
        string[]? allowedExtensions = null
    )
    {
        allowedMimeTypes ??= DefaultAllowedMimeTypes;
        allowedExtensions ??= DefaultAllowedExtensions;

        // Validate file exists
        if (file == null || file.Length == 0)
        {
            return ValidationResult.Fail(
                "No audio file provided",
                "Upload attempt with no file or empty file"
            );
        }

        // Validate file size
        if (file.Length > maxFileSizeBytes)
        {
            return ValidationResult.Fail(
                $"File size exceeds maximum limit of {maxFileSizeBytes / 1_000_000} MB",
                $"Upload attempt with file too large: {file.Length} bytes"
            );
        }

        // Validate MIME type
        if (!allowedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            var allowedFormats = string.Join(", ", allowedMimeTypes);
            return ValidationResult.Fail(
                $"Unsupported audio format. Only {allowedFormats} are allowed. Received: {file.ContentType}",
                $"Upload attempt with unsupported MIME type: {file.ContentType}"
            );
        }

        // Validate file extension
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            var allowedExts = string.Join(", ", allowedExtensions);
            return ValidationResult.Fail(
                $"Unsupported file extension. Only {allowedExts} are allowed",
                $"Upload attempt with unsupported extension: {fileExtension}"
            );
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Logs validation result if logger is provided
    /// </summary>
    public static void LogValidationResult<T>(
        ValidationResult result,
        ILogger<T> logger,
        IFormFile? file = null
    )
    {
        if (!result.IsValid && result.LogMessage != null)
        {
            logger.LogWarning(result.LogMessage);
        }
        else if (result.IsValid && file != null)
        {
            logger.LogInformation(
                "Audio file validation passed: Name={FileName}, Size={Size} bytes, Type={ContentType}",
                file.FileName,
                file.Length,
                file.ContentType
            );
        }
    }
}
