using BookingCare.Services.AI.Services.Interfaces;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for extracting keywords from lab result extracted text
/// Extracts lab indicator names, values, and units for caching
/// </summary>
public class LabResultKeywordExtractor : ILabResultKeywordExtractor
{
    private readonly ILogger<LabResultKeywordExtractor> _logger;
    
    // Common lab indicator names in Vietnamese and English
    private static readonly HashSet<string> LabIndicatorNames = new(StringComparer.OrdinalIgnoreCase)
    {
        // Blood tests
        "wbc", "bạch cầu", "white blood cell", "rbc", "hồng cầu", "red blood cell",
        "hemoglobin", "hgb", "huyết sắc tố", "hematocrit", "hct", "hematocrit",
        "platelet", "tiểu cầu", "plt", "mcv", "mch", "mchc",
        
        // Biochemistry
        "glucose", "đường huyết", "glucose", "creatinine", "creatinin", "urea", "ure",
        "ast", "got", "alt", "gpt", "bilirubin", "bilirubin", "albumin", "albumin",
        "cholesterol", "cholesterol", "triglyceride", "triglycerid", "ldl", "hdl",
        
        // Liver function
        "alt", "sgot", "ast", "sgpt", "ggt", "alkaline phosphatase", "phosphatase",
        
        // Kidney function
        "creatinine", "creatinin", "urea", "ure", "uric acid", "acid uric",
        
        // Lipid profile
        "cholesterol", "cholesterol", "triglyceride", "triglycerid", "ldl", "hdl",
        
        // Other common tests
        "tsh", "t3", "t4", "ft3", "ft4", "psa", "ca125", "ca199", "afp", "cea"
    };
    
    public LabResultKeywordExtractor(ILogger<LabResultKeywordExtractor> logger)
    {
        _logger = logger;
    }
    
    public string ExtractKeywords(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return string.Empty;
        
        var keywords = new HashSet<string>();
        var lowerText = extractedText.ToLowerInvariant();
        
        // Extract lab indicator names
        foreach (var indicator in LabIndicatorNames)
        {
            if (lowerText.Contains(indicator, StringComparison.OrdinalIgnoreCase))
            {
                // Use normalized name (prefer English abbreviation if available)
                var normalizedName = NormalizeIndicatorName(indicator);
                keywords.Add(normalizedName);
            }
        }
        
        // Also extract common patterns: "Name: Value Unit" or "Name Value Unit"
        // This helps catch indicators that might not be in our list
        var lines = extractedText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var lineLower = line.ToLowerInvariant();
            
            // Look for patterns like "WBC: 5.2 x10^9/L" or "Hemoglobin 140 g/L"
            // Extract the indicator name (first word or words before colon/space)
            var colonIndex = lineLower.IndexOf(':');
            var spaceIndex = lineLower.IndexOf(' ');
            
            if (colonIndex > 0)
            {
                var potentialName = lineLower.Substring(0, colonIndex).Trim();
                if (potentialName.Length >= 2 && potentialName.Length <= 30)
                {
                    // Clean up the name (remove common prefixes/suffixes)
                    var cleaned = CleanIndicatorName(potentialName);
                    if (cleaned.Length >= 2)
                    {
                        keywords.Add(cleaned);
                    }
                }
            }
            else if (spaceIndex > 0)
            {
                var potentialName = lineLower.Substring(0, spaceIndex).Trim();
                if (potentialName.Length >= 2 && potentialName.Length <= 30)
                {
                    var cleaned = CleanIndicatorName(potentialName);
                    if (cleaned.Length >= 2)
                    {
                        keywords.Add(cleaned);
                    }
                }
            }
        }
        
        // Normalize and return
        var normalized = NormalizeKeywords(keywords);
        
        _logger.LogDebug(
            "Extracted {Count} keywords from lab result text: {Keywords}",
            keywords.Count,
            normalized);
        
        return normalized;
    }
    
    public string NormalizeText(string extractedText)
    {
        if (string.IsNullOrWhiteSpace(extractedText))
            return string.Empty;
        
        var lowerText = extractedText.ToLowerInvariant();
        
        // Vietnamese stop words to remove
        var stopWords = new HashSet<string>
        {
            "kết quả", "xét nghiệm", "bệnh viện", "phòng", "bệnh nhân", "ngày",
            "tháng", "năm", "giờ", "phút", "bác sĩ", "điều dưỡng",
            "tên", "tuổi", "giới tính", "địa chỉ", "số", "mã", "id",
            "bình thường", "bất thường", "cao", "thấp", "tăng", "giảm",
            "reference", "range", "normal", "abnormal", "high", "low",
            "result", "test", "laboratory", "lab", "hospital", "patient"
        };
        
        // Split by whitespace and punctuation, then filter out stop words
        var words = lowerText
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\t', '\n', '\r', '|', '-' }, 
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !stopWords.Contains(w) && w.Length > 0)
            .ToList();
        
        // Join back with single space and trim
        var normalized = string.Join(" ", words).Trim();
        
        return normalized;
    }
    
    /// <summary>
    /// Normalize indicator name to a canonical form
    /// </summary>
    private string NormalizeIndicatorName(string indicator)
    {
        // Map Vietnamese names to English abbreviations
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bạch cầu", "wbc" },
            { "white blood cell", "wbc" },
            { "hồng cầu", "rbc" },
            { "red blood cell", "rbc" },
            { "huyết sắc tố", "hgb" },
            { "tiểu cầu", "plt" },
            { "platelet", "plt" },
            { "đường huyết", "glucose" },
            { "creatinin", "creatinine" },
            { "ure", "urea" },
            { "cholesterol", "cholesterol" },
            { "triglycerid", "triglyceride" }
        };
        
        if (mapping.TryGetValue(indicator, out var normalized))
        {
            return normalized;
        }
        
        return indicator.ToLowerInvariant();
    }
    
    /// <summary>
    /// Clean indicator name by removing common prefixes/suffixes
    /// </summary>
    private string CleanIndicatorName(string name)
    {
        // Remove common prefixes
        var prefixes = new[] { "chỉ số", "test", "xét nghiệm", "lab", "kết quả" };
        foreach (var prefix in prefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(prefix.Length).Trim();
            }
        }
        
        // Remove common suffixes
        var suffixes = new[] { ":", "=", "-", "(", "[" };
        foreach (var suffix in suffixes)
        {
            if (name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - suffix.Length).Trim();
            }
        }
        
        return name.Trim();
    }
    
    /// <summary>
    /// Normalize keywords: lowercase, sort, join with comma
    /// </summary>
    private string NormalizeKeywords(IEnumerable<string> keywords)
    {
        return string.Join(",", keywords
            .Select(k => k.ToLowerInvariant().Trim())
            .Distinct()
            .OrderBy(k => k));
    }
}

