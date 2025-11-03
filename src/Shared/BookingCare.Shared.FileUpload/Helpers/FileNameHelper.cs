using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BookingCare.Shared.FileUpload.Helpers;

/// <summary>
/// Helper class for sanitizing and normalizing file names for S3 storage
/// </summary>
public static class FileNameHelper
{
    /// <summary>
/// Sanitize filename to be URL-safe and S3-compatible
    /// Removes Unicode characters, special characters, and spaces
    /// </summary>
    /// <param name="fileName">Original filename with extension</param>
    /// <returns>Sanitized filename safe for S3 storage</returns>
    public static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
        return "unnamed_file";
        }

        // Extract extension first
  var extension = Path.GetExtension(fileName);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

  // Sanitize filename part
        var sanitizedName = SanitizeFileNameWithoutExtension(nameWithoutExtension);

  // Combine with original extension (keep extension as-is)
        return $"{sanitizedName}{extension.ToLowerInvariant()}";
    }

    /// <summary>
    /// Sanitize filename without extension
    /// </summary>
    /// <param name="fileNameWithoutExtension">Filename without extension</param>
    /// <returns>Sanitized filename</returns>
    public static string SanitizeFileNameWithoutExtension(string fileNameWithoutExtension)
    {
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
  {
 return "unnamed";
        }

        var sanitized = fileNameWithoutExtension;

        // Step 1: Remove diacritics (accents) from Unicode characters
        // Converts: "H? s?" ? "Ho so"
        sanitized = RemoveDiacritics(sanitized);

        // Step 2: Replace spaces and special characters with underscores
        // Converts: "Ho so benh an" ? "Ho_so_benh_an"
      sanitized = Regex.Replace(sanitized, @"[\s\-]+", "_");

        // Step 3: Remove all non-alphanumeric characters except underscore and dash
        // Keeps only: a-z, A-Z, 0-9, _, -
        sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9_\-]", "");

        // Step 4: Remove multiple consecutive underscores
        sanitized = Regex.Replace(sanitized, @"_{2,}", "_");

        // Step 5: Trim underscores and dashes from start and end
        sanitized = sanitized.Trim('_', '-');

      // Step 6: If empty after sanitization, use fallback
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "file";
        }

        // Step 7: Limit length to 100 characters (S3 recommends shorter keys)
        if (sanitized.Length > 100)
 {
      sanitized = sanitized.Substring(0, 100).TrimEnd('_', '-');
        }

        return sanitized;
    }

    /// <summary>
    /// Remove diacritics (accents) from Unicode characters
    /// Converts: "H? Chí Minh" ? "Ho Chi Minh"
    /// Converts: "Café" ? "Cafe"
    /// Converts: "??????" ? "Moskva" (Cyrillic)
    /// </summary>
    /// <param name="text">Text with diacritics</param>
    /// <returns>Text without diacritics</returns>
    private static string RemoveDiacritics(string text)
    {
 // Normalize Unicode string to FormD (decomposed form)
        // This separates base characters from combining diacritical marks
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

  foreach (var c in normalizedString)
 {
            // Get Unicode category
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

            // Keep all characters except NonSpacingMark (diacritical marks)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
         {
       stringBuilder.Append(c);
       }
        }

        // Normalize back to FormC (composed form)
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Generate a unique filename with timestamp and GUID
    /// </summary>
    /// <param name="originalFileName">Original filename</param>
    /// <returns>Unique sanitized filename</returns>
    public static string GenerateUniqueFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
  var sanitizedName = SanitizeFileNameWithoutExtension(nameWithoutExtension);

        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd");

return $"{sanitizedName}_{timestamp}_{uniqueId}{extension.ToLowerInvariant()}";
    }

    /// <summary>
    /// Validate if filename is safe for S3 storage
    /// </summary>
    /// <param name="fileName">Filename to validate</param>
    /// <returns>True if safe, false otherwise</returns>
    public static bool IsSafeForS3(string fileName)
    {
   if (string.IsNullOrWhiteSpace(fileName))
        {
  return false;
        }

     // Check for unsafe characters that might cause issues in S3
        var unsafeChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0' };
        return !fileName.Any(c => unsafeChars.Contains(c) || c > 127); // No non-ASCII
    }

    /// <summary>
    /// Transliterate common non-English characters to English equivalents
    /// Useful for Cyrillic, Greek, and other scripts
    /// </summary>
    /// <param name="text">Text to transliterate</param>
    /// <returns>Transliterated text</returns>
    public static string Transliterate(string text)
    {
        // Common transliteration mappings
        var transliterationMap = new Dictionary<char, string>
        {
            // Cyrillic
            { '?', "a" }, { '?', "b" }, { '?', "v" }, { '?', "g" }, { '?', "d" },
            { '?', "e" }, { '?', "yo" }, { '?', "zh" }, { '?', "z" }, { '?', "i" },
            { '?', "y" }, { '?', "k" }, { '?', "l" }, { '?', "m" }, { '?', "n" },
    { '?', "o" }, { '?', "p" }, { '?', "r" }, { '?', "s" }, { '?', "t" },
          { '?', "u" }, { '?', "f" }, { '?', "h" }, { '?', "ts" }, { '?', "ch" },
    { '?', "sh" }, { '?', "shch" }, { '?', "" }, { '?', "y" }, { '?', "" },
 { '?', "e" }, { '?', "yu" }, { '?', "ya" },
      { '?', "A" }, { '?', "B" }, { '?', "V" }, { '?', "G" }, { '?', "D" },
     { '?', "E" }, { '?', "Yo" }, { '?', "Zh" }, { '?', "Z" }, { '?', "I" },
         { '?', "Y" }, { '?', "K" }, { '?', "L" }, { '?', "M" }, { '?', "N" },
         { '?', "O" }, { '?', "P" }, { '?', "R" }, { '?', "S" }, { '?', "T" },
       { '?', "U" }, { '?', "F" }, { '?', "H" }, { '?', "Ts" }, { '?', "Ch" },
          { '?', "Sh" }, { '?', "Shch" }, { '?', "" }, { '?', "Y" }, { '?', "" },
            { '?', "E" }, { '?', "Yu" }, { '?', "Ya" },
            
 // Greek
            { '?', "a" }, { '?', "b" }, { '?', "g" }, { '?', "d" }, { '?', "e" },
            { '?', "A" }, { '?', "B" }, { '?', "G" }, { '?', "D" }, { '?', "E" },
      };

    var result = new StringBuilder();
   foreach (var c in text)
        {
            if (transliterationMap.TryGetValue(c, out var replacement))
      {
       result.Append(replacement);
            }
            else
{
     result.Append(c);
            }
        }

  return result.ToString();
    }
}
