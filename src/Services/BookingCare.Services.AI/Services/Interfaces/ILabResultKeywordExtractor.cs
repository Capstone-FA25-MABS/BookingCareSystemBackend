namespace BookingCare.Services.AI.Services.Interfaces;

/// <summary>
/// Service for extracting keywords from lab result extracted text
/// </summary>
public interface ILabResultKeywordExtractor
{
    /// <summary>
    /// Extract keywords from extracted text (OCR result)
    /// Focuses on lab indicator names, values, and units
    /// </summary>
    /// <param name="extractedText">Text extracted from lab result image/PDF</param>
    /// <returns>Normalized keywords string (lowercase, sorted, comma-separated)</returns>
    string ExtractKeywords(string extractedText);
    
    /// <summary>
    /// Normalize extracted text for exact matching: remove stop words, lowercase, normalize whitespace
    /// Used for Tier 0 exact text matching in cache
    /// </summary>
    /// <param name="extractedText">Original extracted text</param>
    /// <returns>Normalized text with stop words removed</returns>
    string NormalizeText(string extractedText);
}

