using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using BookingCare.Services.AI.Data;
using BookingCare.Services.AI.Models.DTOs.Requests;
using BookingCare.Services.AI.Models.Entities;
using BookingCare.Services.AI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.AI.Services.Implementations;

/// <summary>
/// Service for extracting keywords from conversation with context awareness
/// Uses database ConversationContextKeywords for pattern matching
/// </summary>
public class ContextKeywordExtractor : IContextKeywordExtractor
{
    private readonly ILogger<ContextKeywordExtractor> _logger;
    private readonly AiDbContext _dbContext;
    private static Dictionary<string, string[]>? _cachedPatterns;
    private static DateTime? _lastCacheUpdate;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

    // JSON options to preserve Unicode characters (not escape them)
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public ContextKeywordExtractor(
        ILogger<ContextKeywordExtractor> logger,
        AiDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public string ExtractKeywordsWithContext(
        string currentMessage,
        List<ConversationMessage> history)
    {
        var keywords = new HashSet<string>();

        // 1. Extract initial symptom from first user message
        if (history.Count > 0)
        {
            var initialMessage = history
                .FirstOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase));

            if (initialMessage != null)
            {
                var initialSymptoms = ExtractSymptoms(initialMessage.Content, autoAddToDb: true);
                keywords.UnionWith(initialSymptoms);
            }
        }

        // Also extract from current message
        var currentSymptoms = ExtractSymptoms(currentMessage, autoAddToDb: true);
        keywords.UnionWith(currentSymptoms);

        // 2. Extract context from user answers (skip first message)
        var userAnswers = history
            .Where(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            .Skip(1) // Skip initial symptom
            .Select(m => m.Content.ToLowerInvariant())
            .ToList();

        foreach (var answer in userAnswers)
        {
            // Extract location
            var location = ExtractPatternFromDb(answer, "LOCATION");
            if (location != null) keywords.Add(location);

            // Extract intensity
            var intensity = ExtractPatternFromDb(answer, "INTENSITY");
            if (intensity != null) keywords.Add(intensity);

            // Extract additional symptoms mentioned in answers
            var symptoms = ExtractSymptoms(answer, autoAddToDb: true);
            keywords.UnionWith(symptoms);
        }

        // 3. Normalize and return
        var normalized = NormalizeKeywords(keywords);

        _logger.LogDebug(
            "Extracted keywords: {Keywords} from {MessageCount} messages",
            normalized,
            history.Count);

        return normalized;
    }

    public string ExtractInitialSymptom(string message)
    {
        var symptoms = ExtractSymptoms(message, autoAddToDb: true);
        return NormalizeKeywords(symptoms);
    }

    public List<string> ExtractContextFromAnswers(List<ConversationMessage> history)
    {
        var context = new List<string>();

        // Extract from initial symptom (first user message)
        var initialMessage = history
            .FirstOrDefault(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase));

        if (initialMessage != null)
        {
            var initialSymptoms = ExtractSymptoms(initialMessage.Content, autoAddToDb: true);
            context.AddRange(initialSymptoms);
        }

        // Extract from user answers (skip first message)
        var userAnswers = history
            .Where(m => m.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            .Skip(1) // Skip initial message
            .Select(m => m.Content.ToLowerInvariant());

        foreach (var answer in userAnswers)
        {
            // Extract location
            var location = ExtractPatternFromDb(answer, "LOCATION");
            if (location != null) context.Add(location);

            // Extract intensity
            var intensity = ExtractPatternFromDb(answer, "INTENSITY");
            if (intensity != null) context.Add(intensity);

            // Extract additional symptoms mentioned in answers
            var symptoms = ExtractSymptoms(answer, autoAddToDb: true);
            context.AddRange(symptoms);
        }

        return context.Distinct().ToList();
    }

    /// <summary>
    /// Load patterns from database with caching
    /// </summary>
    private async Task<Dictionary<string, string[]>> LoadPatternsFromDbAsync()
    {
        // Check cache
        if (_cachedPatterns != null && _lastCacheUpdate.HasValue &&
            DateTime.UtcNow - _lastCacheUpdate.Value < CacheExpiry)
        {
            return _cachedPatterns;
        }

        try
        {
            var keywords = await _dbContext.ConversationContextKeywords.ToListAsync();
            _cachedPatterns = new Dictionary<string, string[]>();

            foreach (var keyword in keywords)
            {
                try
                {
                    var synonyms = string.IsNullOrEmpty(keyword.Synonyms)
                        ? new[] { keyword.Keyword }
                        : JsonSerializer.Deserialize<string[]>(keyword.Synonyms) ?? new[] { keyword.Keyword };

                    _cachedPatterns[keyword.Keyword] = synonyms;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse synonyms for keyword {Keyword}, using keyword only", keyword.Keyword);
                    _cachedPatterns[keyword.Keyword] = new[] { keyword.Keyword };
                }
            }

            _lastCacheUpdate = DateTime.UtcNow;
            _logger.LogInformation("Loaded {Count} keywords from database", _cachedPatterns.Count);

            return _cachedPatterns;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load keywords from database, using empty patterns");
            return new Dictionary<string, string[]>();
        }
    }

    /// <summary>
    /// Extract symptom keywords from text using database patterns
    /// If symptom not found in DB and autoAddToDb is true, automatically add it
    /// </summary>
    private List<string> ExtractSymptoms(string text, bool autoAddToDb = false)
    {
        var symptoms = new List<string>();
        var lowerText = text.ToLowerInvariant();
        var foundKeywords = new HashSet<string>();
        var matchedPositions = new Dictionary<string, int>(); // Track match positions to avoid overlapping

        try
        {
            // Only extract SYMPTOM category - query synchronously (should be fast with index)
            var symptomKeywords = _dbContext.ConversationContextKeywords
                .Where(k => k.Category == "SYMPTOM")
                .ToList();

            // Sort by length (longest first) to prioritize multi-word phrases (e.g., "đau lưng" before "đau")
            var sortedKeywords = symptomKeywords
                .OrderByDescending(k => k.Keyword.Length)
                .ToList();

            // First pass: Match multi-word phrases (2+ words) first
            foreach (var keyword in sortedKeywords.Where(k => k.Keyword.Contains(' ')))
            {
                try
                {
                    var synonyms = string.IsNullOrEmpty(keyword.Synonyms)
                        ? new[] { keyword.Keyword }
                        : JsonSerializer.Deserialize<string[]>(keyword.Synonyms) ?? new[] { keyword.Keyword };

                    foreach (var synonym in synonyms)
                    {
                        var synonymLower = synonym.ToLowerInvariant();
                        var index = lowerText.IndexOf(synonymLower, StringComparison.OrdinalIgnoreCase);

                        if (index >= 0)
                        {
                            // Check if this position overlaps with already matched keywords
                            var overlaps = matchedPositions.Any(kvp =>
                                (index >= kvp.Value && index < kvp.Value + kvp.Key.Length) ||
                                (kvp.Value >= index && kvp.Value < index + synonymLower.Length));

                            if (!overlaps)
                            {
                                var keywordLower = keyword.Keyword.ToLowerInvariant();
                                if (!foundKeywords.Contains(keywordLower))
                                {
                                    symptoms.Add(keyword.Keyword);
                                    foundKeywords.Add(keywordLower);
                                    matchedPositions[keyword.Keyword] = index;

                                    _logger.LogDebug("Matched multi-word symptom: '{Symptom}' at position {Index}", keyword.Keyword, index);
                                    break; // Found match, move to next keyword
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // If JSON parsing fails, just check the keyword itself
                    var keywordLower = keyword.Keyword.ToLowerInvariant();
                    var index = lowerText.IndexOf(keywordLower, StringComparison.OrdinalIgnoreCase);

                    if (index >= 0)
                    {
                        var overlaps = matchedPositions.Any(kvp =>
                            (index >= kvp.Value && index < kvp.Value + kvp.Key.Length) ||
                            (kvp.Value >= index && kvp.Value < index + keywordLower.Length));

                        if (!overlaps && !foundKeywords.Contains(keywordLower))
                        {
                            symptoms.Add(keyword.Keyword);
                            foundKeywords.Add(keywordLower);
                            matchedPositions[keyword.Keyword] = index;
                        }
                    }
                }
            }

            // Second pass: Match single-word keywords only if they don't overlap with matched phrases
            // Store single-word matches temporarily to check for multi-word phrases later
            var singleWordMatches = new List<(string keyword, int index)>();

            foreach (var keyword in sortedKeywords.Where(k => !k.Keyword.Contains(' ')))
            {
                try
                {
                    var synonyms = string.IsNullOrEmpty(keyword.Synonyms)
                        ? new[] { keyword.Keyword }
                        : JsonSerializer.Deserialize<string[]>(keyword.Synonyms) ?? new[] { keyword.Keyword };

                    foreach (var synonym in synonyms)
                    {
                        var synonymLower = synonym.ToLowerInvariant();
                        var index = lowerText.IndexOf(synonymLower, StringComparison.OrdinalIgnoreCase);

                        if (index >= 0)
                        {
                            // Check if this position overlaps with already matched keywords
                            var overlaps = matchedPositions.Any(kvp =>
                                (index >= kvp.Value && index < kvp.Value + kvp.Key.Length) ||
                                (kvp.Value >= index && kvp.Value < index + synonymLower.Length));

                            if (!overlaps)
                            {
                                var keywordLower = keyword.Keyword.ToLowerInvariant();
                                if (!foundKeywords.Contains(keywordLower))
                                {
                                    // Store for potential multi-word phrase check
                                    singleWordMatches.Add((keyword.Keyword, index));
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    var keywordLower = keyword.Keyword.ToLowerInvariant();
                    var index = lowerText.IndexOf(keywordLower, StringComparison.OrdinalIgnoreCase);

                    if (index >= 0)
                    {
                        var overlaps = matchedPositions.Any(kvp =>
                            (index >= kvp.Value && index < kvp.Value + kvp.Key.Length) ||
                            (kvp.Value >= index && kvp.Value < index + keywordLower.Length));

                        if (!overlaps && !foundKeywords.Contains(keywordLower))
                        {
                            singleWordMatches.Add((keyword.Keyword, index));
                        }
                    }
                }
            }

            // Before adding single-word matches, check if we can find multi-word phrases containing them
            // This handles cases like "đau" being matched but "đau răng" should be preferred
            if (autoAddToDb && singleWordMatches.Count > 0)
            {
                var commonSymptomPatterns = new[] { "đau", "sưng", "tê", "ngứa", "nóng", "lạnh", "chảy", "ho", "sốt" };
                var commonBodyParts = new[] {
                    "chân", "tay", "đầu", "bụng", "lưng", "ngực", "cổ", "mắt", "tai", "mũi", "họng",
                    "răng", "lợi", "miệng", "hàm", "gót", "gối", "khuỷu", "vai", "cổ tay", "cổ chân",
                    "thắt lưng", "vùng chậu", "ngón tay", "ngón chân", "bàn tay", "bàn chân"
                };

                // Check if any single-word match can form a multi-word phrase
                foreach (var (matchedKeyword, matchedIndex) in singleWordMatches.ToList())
                {
                    var matchedKeywordLower = matchedKeyword.ToLowerInvariant();

                    // Check if this keyword is a symptom pattern that could combine with body parts
                    if (commonSymptomPatterns.Contains(matchedKeywordLower))
                    {
                        // Look for body parts immediately after this keyword in the text
                        // Check if the phrase appears as a contiguous string in the original text
                        foreach (var bodyPart in commonBodyParts)
                        {
                            var potentialPhrase = $"{matchedKeywordLower} {bodyPart}";

                            // First check if the phrase appears in the original text as a contiguous string
                            if (!lowerText.Contains(potentialPhrase, StringComparison.OrdinalIgnoreCase))
                            {
                                continue; // Skip if phrase doesn't appear in text
                            }

                            // Check if this phrase exists in DB or should be added
                            var allSymptoms = _dbContext.ConversationContextKeywords
                                .Where(k => k.Category == "SYMPTOM")
                                .ToList();

                            var exists = allSymptoms.Any(k =>
                                k.Keyword.Equals(potentialPhrase, StringComparison.OrdinalIgnoreCase));

                            if (exists)
                            {
                                // Phrase exists, use it instead of single word
                                var existingKeyword = allSymptoms.FirstOrDefault(k =>
                                    k.Keyword.Equals(potentialPhrase, StringComparison.OrdinalIgnoreCase));
                                if (existingKeyword != null && !foundKeywords.Contains(potentialPhrase))
                                {
                                    // Remove single-word match
                                    singleWordMatches.RemoveAll(m => m.keyword == matchedKeyword);

                                    // Add multi-word phrase
                                    symptoms.Add(existingKeyword.Keyword);
                                    foundKeywords.Add(potentialPhrase);
                                    matchedPositions[existingKeyword.Keyword] = matchedIndex;

                                    _logger.LogDebug("Upgraded '{Single}' to '{Phrase}'", matchedKeyword, potentialPhrase);
                                    break;
                                }
                            }
                            else
                            {
                                // Phrase doesn't exist in DB but appears in text, add it
                                try
                                {
                                    var newKeyword = new ConversationContextKeywordEntity
                                    {
                                        Id = Guid.NewGuid(),
                                        Keyword = potentialPhrase,
                                        Category = "SYMPTOM",
                                        Synonyms = JsonSerializer.Serialize(new[] { potentialPhrase }, JsonOptions),
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    _dbContext.ConversationContextKeywords.Add(newKeyword);
                                    _dbContext.SaveChanges();

                                    // Remove single-word match
                                    singleWordMatches.RemoveAll(m => m.keyword == matchedKeyword);

                                    // Add multi-word phrase
                                    symptoms.Add(potentialPhrase);
                                    foundKeywords.Add(potentialPhrase);
                                    matchedPositions[potentialPhrase] = matchedIndex;

                                    _logger.LogInformation(
                                        "Auto-added and upgraded '{Single}' to '{Phrase}' in ConversationContextKeywords",
                                        matchedKeyword, potentialPhrase);
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to auto-add symptom phrase '{Symptom}' to database", potentialPhrase);
                                }
                            }
                        }
                    }
                }
            }

            // Add remaining single-word matches that weren't upgraded to phrases
            foreach (var (keyword, index) in singleWordMatches)
            {
                if (!foundKeywords.Contains(keyword.ToLowerInvariant()))
                {
                    symptoms.Add(keyword);
                    foundKeywords.Add(keyword.ToLowerInvariant());
                    matchedPositions[keyword] = index;
                }
            }

            // If autoAddToDb is true and we still haven't found any symptoms, try intelligent extraction
            if (autoAddToDb && symptoms.Count == 0)
            {
                // First, try to extract multi-word phrases (e.g., "đau chân", "đau đầu", "đau răng")
                // Common symptom patterns: "đau [body part]", "[symptom] [location]"
                var commonSymptomPatterns = new[] { "đau", "sưng", "tê", "ngứa", "nóng", "lạnh", "chảy", "ho", "sốt" };
                var commonBodyParts = new[] {
                    "chân", "tay", "đầu", "bụng", "lưng", "ngực", "cổ", "mắt", "tai", "mũi", "họng",
                    "răng", "lợi", "miệng", "hàm", "gót", "gối", "khuỷu", "vai", "cổ tay", "cổ chân",
                    "thắt lưng", "vùng chậu", "ngón tay", "ngón chân", "bàn tay", "bàn chân"
                };

                // Try to find multi-word symptoms first (e.g., "đau chân", "đau đầu", "đau răng")
                // Strategy: Look for patterns like "đau [body part]" or "[symptom] [body part]"
                var foundPhrase = false;

                // First, try hardcoded patterns (fast path)
                foreach (var pattern in commonSymptomPatterns)
                {
                    foreach (var bodyPart in commonBodyParts)
                    {
                        var phrase = $"{pattern} {bodyPart}";
                        if (lowerText.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                        {
                            var normalizedPhrase = phrase.Trim();

                            // Check if already exists
                            var allSymptoms = _dbContext.ConversationContextKeywords
                                .Where(k => k.Category == "SYMPTOM")
                                .ToList();

                            var exists = allSymptoms.Any(k =>
                                k.Keyword.Equals(normalizedPhrase, StringComparison.OrdinalIgnoreCase));

                            if (!exists)
                            {
                                try
                                {
                                    var newKeyword = new ConversationContextKeywordEntity
                                    {
                                        Id = Guid.NewGuid(),
                                        Keyword = normalizedPhrase,
                                        Category = "SYMPTOM",
                                        Synonyms = JsonSerializer.Serialize(new[] { normalizedPhrase }, JsonOptions),
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    _dbContext.ConversationContextKeywords.Add(newKeyword);
                                    _dbContext.SaveChanges();

                                    symptoms.Add(normalizedPhrase);
                                    foundKeywords.Add(normalizedPhrase.ToLowerInvariant());
                                    foundPhrase = true;

                                    _logger.LogInformation(
                                        "Auto-added new symptom phrase '{Symptom}' to ConversationContextKeywords",
                                        normalizedPhrase);
                                    break; // Found a phrase, stop looking
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to auto-add symptom phrase '{Symptom}' to database", normalizedPhrase);
                                }
                            }
                            else
                            {
                                // Phrase exists, add it to symptoms
                                var existingKeyword = allSymptoms.FirstOrDefault(k =>
                                    k.Keyword.Equals(normalizedPhrase, StringComparison.OrdinalIgnoreCase));
                                if (existingKeyword != null)
                                {
                                    symptoms.Add(existingKeyword.Keyword);
                                    foundKeywords.Add(existingKeyword.Keyword.ToLowerInvariant());
                                    foundPhrase = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (foundPhrase) break;
                }

                // If no hardcoded pattern found, try to extract multi-word symptom phrases intelligently
                // Look for patterns like: "bị [symptom] [body part]" or "[symptom] [body part]"
                if (!foundPhrase)
                {
                    // Remove common stop words and extract potential symptom phrases
                    var stopWords = new HashSet<string> { "tôi", "bị", "có", "bạn", "anh", "chị", "em", "ông", "bà" };
                    var words = lowerText.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => !stopWords.Contains(w))
                        .ToList();

                    // Look for 2-word combinations that might be symptoms
                    // Common pattern: symptom word + body part word
                    for (int i = 0; i < words.Count - 1; i++)
                    {
                        var word1 = words[i];
                        var word2 = words[i + 1];
                        var potentialPhrase = $"{word1} {word2}";

                        // Check if this phrase appears in the original text
                        if (lowerText.Contains(potentialPhrase, StringComparison.OrdinalIgnoreCase) &&
                            word1.Length >= 3 && word2.Length >= 3 &&
                            !IsCommonWord(word1) && !IsCommonWord(word2))
                        {
                            // Check if already exists in DB
                            var allSymptoms = _dbContext.ConversationContextKeywords
                                .Where(k => k.Category == "SYMPTOM")
                                .ToList();

                            var exists = allSymptoms.Any(k =>
                                k.Keyword.Equals(potentialPhrase, StringComparison.OrdinalIgnoreCase));

                            if (!exists && !foundKeywords.Contains(potentialPhrase.ToLowerInvariant()))
                            {
                                try
                                {
                                    var newKeyword = new ConversationContextKeywordEntity
                                    {
                                        Id = Guid.NewGuid(),
                                        Keyword = potentialPhrase,
                                        Category = "SYMPTOM",
                                        Synonyms = JsonSerializer.Serialize(new[] { potentialPhrase }, JsonOptions),
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    _dbContext.ConversationContextKeywords.Add(newKeyword);
                                    _dbContext.SaveChanges();

                                    symptoms.Add(potentialPhrase);
                                    foundKeywords.Add(potentialPhrase.ToLowerInvariant());
                                    foundPhrase = true;

                                    _logger.LogInformation(
                                        "Auto-added new symptom phrase '{Symptom}' to ConversationContextKeywords (intelligent extraction)",
                                        potentialPhrase);
                                    break; // Found a phrase, stop looking
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to auto-add symptom phrase '{Symptom}' to database", potentialPhrase);
                                }
                            }
                            else if (exists)
                            {
                                // Phrase exists, add it to symptoms
                                var existingKeyword = allSymptoms.FirstOrDefault(k =>
                                    k.Keyword.Equals(potentialPhrase, StringComparison.OrdinalIgnoreCase));
                                if (existingKeyword != null)
                                {
                                    symptoms.Add(existingKeyword.Keyword);
                                    foundKeywords.Add(existingKeyword.Keyword.ToLowerInvariant());
                                    foundPhrase = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // If no phrase found, fall back to single words
                if (!foundPhrase)
                {
                    var words = lowerText.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
                        .Where(w => w != "bị" && w != "có" && w != "tôi" && w != "bạn")
                        .ToList();
                    var potentialSymptoms = words.Where(w => w.Length >= 3 && !IsCommonWord(w)).Take(3).ToList();

                    foreach (var potentialSymptom in potentialSymptoms)
                    {
                        var normalizedSymptom = potentialSymptom.Trim();
                        if (normalizedSymptom.Length >= 3 && !foundKeywords.Contains(normalizedSymptom))
                        {
                            // Check if already exists (case-insensitive)
                            var allSymptoms = _dbContext.ConversationContextKeywords
                                .Where(k => k.Category == "SYMPTOM")
                                .ToList();

                            var exists = allSymptoms.Any(k =>
                                k.Keyword.Equals(normalizedSymptom, StringComparison.OrdinalIgnoreCase));

                            if (!exists)
                            {
                                try
                                {
                                    var newKeyword = new ConversationContextKeywordEntity
                                    {
                                        Id = Guid.NewGuid(),
                                        Keyword = normalizedSymptom,
                                        Category = "SYMPTOM",
                                        Synonyms = JsonSerializer.Serialize(new[] { normalizedSymptom }, JsonOptions),
                                        CreatedAt = DateTime.UtcNow
                                    };

                                    _dbContext.ConversationContextKeywords.Add(newKeyword);
                                    _dbContext.SaveChanges();

                                    symptoms.Add(normalizedSymptom);
                                    foundKeywords.Add(normalizedSymptom);

                                    _logger.LogInformation(
                                        "Auto-added new symptom '{Symptom}' to ConversationContextKeywords",
                                        normalizedSymptom);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to auto-add symptom '{Symptom}' to database", normalizedSymptom);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract symptoms from database, returning empty list");
        }

        return symptoms;
    }

    /// <summary>
    /// Check if word is a common Vietnamese word (not a symptom)
    /// </summary>
    private static bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string>
        {
            "của", "và", "là", "có", "được", "cho", "với", "từ", "về", "trong",
            "tôi", "bạn", "anh", "chị", "em", "ông", "bà", "cô", "chú",
            "đã", "sẽ", "đang", "rất", "quá", "nhiều", "ít", "một", "hai", "ba"
        };
        return commonWords.Contains(word.ToLowerInvariant());
    }

    /// <summary>
    /// Extract pattern (location/intensity) from text using database
    /// Returns canonical form if found
    /// </summary>
    private string? ExtractPatternFromDb(string text, string category)
    {
        var keywords = _dbContext.ConversationContextKeywords
            .Where(k => k.Category == category)
            .ToList();

        var lowerText = text.ToLowerInvariant();

        foreach (var keyword in keywords)
        {
            try
            {
                var synonyms = string.IsNullOrEmpty(keyword.Synonyms)
                    ? new[] { keyword.Keyword }
                    : JsonSerializer.Deserialize<string[]>(keyword.Synonyms) ?? new[] { keyword.Keyword };

                if (synonyms.Any(s => lowerText.Contains(s, StringComparison.OrdinalIgnoreCase)))
                {
                    return keyword.Keyword;
                }
            }
            catch (JsonException)
            {
                // If JSON parsing fails, just check the keyword itself
                if (lowerText.Contains(keyword.Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return keyword.Keyword;
                }
            }
        }

        return null;
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

    /// <summary>
    /// Normalize message for exact matching: remove stop words, lowercase, normalize whitespace
    /// Used for Tier 0 exact message matching in cache
    /// Example: "tôi bị đau răng" → "bị đau răng"
    /// </summary>
    public string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var lowerText = message.ToLowerInvariant();

        // Vietnamese stop words to remove (pronouns and common words)
        var stopWords = new HashSet<string>
        {
            "tôi", "bạn", "anh", "chị", "em", "ông", "bà", "cô", "chú",
            "đã", "sẽ", "đang", "rất", "quá", "nhiều", "ít"
        };

        // Split by whitespace and punctuation, then filter out stop words
        var words = lowerText
            .Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !stopWords.Contains(w) && w.Length > 0)
            .ToList();

        // Join back with single space and trim
        var normalized = string.Join(" ", words).Trim();

        return normalized;
    }
}
