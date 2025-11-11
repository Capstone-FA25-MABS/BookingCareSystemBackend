using System.Text;

namespace BookingCare.Shared.FileUpload.Helpers;

/// <summary>
/// Helper class for handling file content encoding issues
/// </summary>
public static class EncodingHelper
{
    /// <summary>
    /// Detect encoding of text content
    /// </summary>
    /// <param name="content">Text content to analyze</param>
    /// <returns>Detected encoding</returns>
    public static Encoding DetectEncoding(byte[] content)
    {
        // Check for BOM (Byte Order Mark)
        if (content.Length >= 3)
        {
            // UTF-8 BOM: EF BB BF
            if (content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            {
                return Encoding.UTF8;
            }

            // UTF-16 LE BOM: FF FE
            if (content[0] == 0xFF && content[1] == 0xFE)
            {
                return Encoding.Unicode; // UTF-16 Little Endian
            }

            // UTF-16 BE BOM: FE FF
            if (content[0] == 0xFE && content[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode; // UTF-16 Big Endian
            }
        }

        // Check for UTF-8 without BOM (more complex heuristic)
        if (IsLikelyUtf8(content))
        {
            return new UTF8Encoding(false); // UTF-8 without BOM
        }

        // Default to UTF-8 (safest assumption for modern files)
        return new UTF8Encoding(false);
    }

    /// <summary>
    /// Check if content is likely UTF-8 encoded
    /// </summary>
    private static bool IsLikelyUtf8(byte[] content)
    {
        try
        {
            var decoder = Encoding.UTF8.GetDecoder();
            var chars = new char[content.Length];

            decoder.GetChars(content, 0, content.Length, chars, 0, true);

            // If no exception thrown, likely valid UTF-8
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>
    /// Convert text content to UTF-8 if needed
    /// </summary>
    /// <param name="content">Original content</param>
    /// <param name="sourceEncoding">Source encoding (if known)</param>
    /// <returns>UTF-8 encoded content</returns>
    public static byte[] EnsureUtf8(byte[] content, Encoding? sourceEncoding = null)
    {
        if (sourceEncoding == null)
        {
            sourceEncoding = DetectEncoding(content);
        }

        // If already UTF-8, return as-is
        if (sourceEncoding.CodePage == Encoding.UTF8.CodePage)
        {
            return content;
        }

        // Convert to UTF-8
        var text = sourceEncoding.GetString(content);
        return Encoding.UTF8.GetBytes(text);
    }

    /// <summary>
    /// Read text file with proper encoding detection
    /// </summary>
    /// <param name="stream">Input stream</param>
    /// <returns>Text content with proper encoding</returns>
    public static async Task<string> ReadTextWithEncodingDetectionAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        var encoding = DetectEncoding(bytes);
        return encoding.GetString(bytes);
    }

    /// <summary>
    /// Fix common encoding issues in Vietnamese text
    /// </summary>
    /// <param name="text">Text with potential encoding issues</param>
    /// <returns>Fixed text</returns>
    public static string FixVietnameseEncoding(string text)
    {
        // Common issue: UTF-8 bytes interpreted as Latin-1/Windows-1252
        // Example: "vá»›i" should be "v?i"

        // Detect if text has mojibake (garbled text)
        if (!HasMojibake(text))
        {
            return text;
        }

        try
        {
            // Try to fix by converting Latin-1 -> UTF-8
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            // If conversion fails, return original
            return text;
        }
    }

    /// <summary>
    /// Check if text contains mojibake (garbled characters)
    /// </summary>
    private static bool HasMojibake(string text)
    {
        // Common mojibake patterns for Vietnamese
        var mojibakePatterns = new[]
        {
     "á»", "Ä", "Æ¡", "Æ°", // Vietnamese diacritics as mojibake
            "â€", "â€™", "â€œ", // Smart quotes as mojibake
            "Ã", "Â", "È", // Common UTF-8 mojibake prefixes
    };

        return mojibakePatterns.Any(pattern => text.Contains(pattern));
    }

    /// <summary>
    /// Get recommended charset for Content-Type header
    /// </summary>
    /// <param name="contentType">MIME type</param>
    /// <returns>Content-Type with charset</returns>
    public static string GetContentTypeWithCharset(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return "application/octet-stream";
        }

        // If already has charset, return as-is
        if (contentType.Contains("charset=", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        // Add UTF-8 charset for text-based content types
        var textBasedTypes = new[]
        {
      "text/",
          "application/json",
    "application/xml",
            "application/javascript",
   "application/xhtml+xml"
        };

        if (textBasedTypes.Any(type => contentType.StartsWith(type, StringComparison.OrdinalIgnoreCase)))
        {
            return $"{contentType}; charset=utf-8";
        }

        return contentType;
    }

    /// <summary>
    /// Validate if text is properly UTF-8 encoded
    /// </summary>
    /// <param name="text">Text to validate</param>
    /// <returns>True if valid UTF-8</returns>
    public static bool IsValidUtf8(string text)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var decoded = Encoding.UTF8.GetString(bytes);
            return text == decoded;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get BOM (Byte Order Mark) for encoding
    /// </summary>
    /// <param name="encoding">Target encoding</param>
    /// <returns>BOM bytes</returns>
    public static byte[] GetBom(Encoding encoding)
    {
        return encoding.GetPreamble();
    }

    /// <summary>
    /// Add UTF-8 BOM to content if needed
    /// </summary>
    /// <param name="content">Original content</param>
    /// <param name="addBom">Whether to add BOM</param>
    /// <returns>Content with optional BOM</returns>
    public static byte[] AddUtf8Bom(byte[] content, bool addBom = false)
    {
        if (!addBom)
        {
            return content;
        }

        var bom = Encoding.UTF8.GetPreamble();
        var result = new byte[bom.Length + content.Length];

        Buffer.BlockCopy(bom, 0, result, 0, bom.Length);
        Buffer.BlockCopy(content, 0, result, bom.Length, content.Length);

        return result;
    }
}
