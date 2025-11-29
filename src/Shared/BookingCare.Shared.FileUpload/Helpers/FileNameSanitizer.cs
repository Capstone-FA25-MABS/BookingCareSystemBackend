using System.Text;

namespace BookingCare.Shared.FileUpload.Helpers;

/// <summary>
/// Helper class for sanitizing file names to prevent non-ASCII character issues in HTTP headers
/// </summary>
public static class FileNameSanitizer
{
    /// <summary>
    /// Sanitizes file name by removing non-ASCII characters and Vietnamese diacritics
    /// This prevents "Request headers must contain only ASCII characters" errors when uploading to S3
    /// </summary>
    /// <param name="fileName">Original file name</param>
    /// <returns>Sanitized file name with only ASCII characters</returns>
    public static string Sanitize(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return $"file_{Guid.NewGuid():N}";
        }

        // Get file extension
        var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Remove diacritics (Vietnamese accents)
        var normalizedString = nameWithoutExtension.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                // Keep only ASCII characters (letters, digits, dash, underscore)
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.')
                {
                    stringBuilder.Append(c);
                }
                else if (c == ' ')
                {
                    stringBuilder.Append('_');
                }
            }
        }

        var sanitizedName = stringBuilder.ToString();

        // If sanitization resulted in empty string, generate a unique name
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            sanitizedName = $"file_{Guid.NewGuid():N}";
        }

        return sanitizedName + extension;
    }
}
