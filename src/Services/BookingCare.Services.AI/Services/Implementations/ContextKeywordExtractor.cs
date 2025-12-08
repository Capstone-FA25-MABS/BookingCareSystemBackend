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
    private Dictionary<string, string[]>? _cachedPatterns;
    private DateTime? _lastCacheUpdate;
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
        var matchedPositions = new Dictionary<string, int>();

        try
        {
            var sortedKeywords = GetSortedSymptomKeywords();

            MatchMultiWordPhrases(sortedKeywords, lowerText, symptoms, foundKeywords, matchedPositions);
            var singleWordMatches = MatchSingleWordKeywords(sortedKeywords, lowerText, matchedPositions, foundKeywords);

            if (autoAddToDb && singleWordMatches.Count > 0)
            {
                UpgradeSingleWordToPhrases(singleWordMatches, lowerText, symptoms, foundKeywords, matchedPositions);
            }

            AddRemainingSingleWordMatches(singleWordMatches, symptoms, foundKeywords, matchedPositions);

            if (autoAddToDb && symptoms.Count == 0)
            {
                TryIntelligentExtraction(lowerText, symptoms, foundKeywords);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract symptoms from database, returning empty list");
        }

        return symptoms;
    }

    /// <summary>
    /// Try to match a keyword with its synonyms in the text
    /// </summary>
    private bool TryMatchKeywordWithSynonyms(
        ConversationContextKeywordEntity keyword,
        string lowerText,
        Dictionary<string, int> matchedPositions,
        HashSet<string> foundKeywords,
        Action<string, int> onMatch)
    {
        try
        {
            var synonyms = ParseSynonyms(keyword);
            return TryMatchWithSynonyms(keyword, synonyms, lowerText, matchedPositions, foundKeywords, onMatch);
        }
        catch (JsonException)
        {
            return TryMatchKeywordOnly(keyword, lowerText, matchedPositions, foundKeywords, onMatch);
        }
    }

    private static string[] ParseSynonyms(ConversationContextKeywordEntity keyword)
    {
        return string.IsNullOrEmpty(keyword.Synonyms)
            ? new[] { keyword.Keyword }
            : JsonSerializer.Deserialize<string[]>(keyword.Synonyms) ?? new[] { keyword.Keyword };
    }

    private static bool TryMatchWithSynonyms(
        ConversationContextKeywordEntity keyword,
        string[] synonyms,
        string lowerText,
        Dictionary<string, int> matchedPositions,
        HashSet<string> foundKeywords,
        Action<string, int> onMatch)
    {
        foreach (var synonym in synonyms)
        {
            var synonymLower = synonym.ToLowerInvariant();
            var index = lowerText.IndexOf(synonymLower, StringComparison.OrdinalIgnoreCase);

            if (index < 0) continue;

            if (IsValidMatch(keyword.Keyword, index, synonymLower.Length, matchedPositions, foundKeywords))
            {
                onMatch(keyword.Keyword, index);
                return true;
            }
        }

        return false;
    }

    private static bool TryMatchKeywordOnly(
        ConversationContextKeywordEntity keyword,
        string lowerText,
        Dictionary<string, int> matchedPositions,
        HashSet<string> foundKeywords,
        Action<string, int> onMatch)
    {
        var keywordLower = keyword.Keyword.ToLowerInvariant();
        var index = lowerText.IndexOf(keywordLower, StringComparison.OrdinalIgnoreCase);

        if (index < 0) return false;

        if (IsValidMatch(keyword.Keyword, index, keywordLower.Length, matchedPositions, foundKeywords))
        {
            onMatch(keyword.Keyword, index);
            return true;
        }

        return false;
    }

    private static bool IsValidMatch(
        string keyword,
        int index,
        int length,
        Dictionary<string, int> matchedPositions,
        HashSet<string> foundKeywords)
    {
        if (HasOverlap(index, length, matchedPositions))
        {
            return false;
        }

        var keywordLower = keyword.ToLowerInvariant();
        return !foundKeywords.Contains(keywordLower);
    }

    private static bool HasOverlap(int index, int length, Dictionary<string, int> matchedPositions)
    {
        return matchedPositions.Any(kvp =>
            (index >= kvp.Value && index < kvp.Value + kvp.Key.Length) ||
            (kvp.Value >= index && kvp.Value < index + length));
    }

    private List<ConversationContextKeywordEntity> GetSortedSymptomKeywords()
    {
        var symptomKeywords = _dbContext.ConversationContextKeywords
            .Where(k => k.Category == "SYMPTOM")
            .ToList();

        return symptomKeywords
            .OrderByDescending(k => k.Keyword.Length)
            .ToList();
    }

    private void MatchMultiWordPhrases(
        List<ConversationContextKeywordEntity> sortedKeywords,
        string lowerText,
        List<string> symptoms,
        HashSet<string> foundKeywords,
        Dictionary<string, int> matchedPositions)
    {
        foreach (var keyword in sortedKeywords.Where(k => k.Keyword.Contains(' ')))
        {
            TryMatchKeywordWithSynonyms(
                keyword,
                lowerText,
                matchedPositions,
                foundKeywords,
                (matchedKeyword, matchedIndex) =>
                {
                    symptoms.Add(matchedKeyword);
                    foundKeywords.Add(matchedKeyword.ToLowerInvariant());
                    matchedPositions[matchedKeyword] = matchedIndex;
                    _logger.LogDebug("Matched multi-word symptom: '{Symptom}' at position {Index}", matchedKeyword, matchedIndex);
                });
        }
    }

    private List<(string keyword, int index)> MatchSingleWordKeywords(
        List<ConversationContextKeywordEntity> sortedKeywords,
        string lowerText,
        Dictionary<string, int> matchedPositions,
        HashSet<string> foundKeywords)
    {
        var singleWordMatches = new List<(string keyword, int index)>();

        foreach (var keyword in sortedKeywords.Where(k => !k.Keyword.Contains(' ')))
        {
            TryMatchKeywordWithSynonyms(
                keyword,
                lowerText,
                matchedPositions,
                foundKeywords,
                (matchedKeyword, matchedIndex) =>
                {
                    singleWordMatches.Add((matchedKeyword, matchedIndex));
                });
        }

        return singleWordMatches;
    }

    private void UpgradeSingleWordToPhrases(
        List<(string keyword, int index)> singleWordMatches,
        string lowerText,
        List<string> symptoms,
        HashSet<string> foundKeywords,
        Dictionary<string, int> matchedPositions)
    {
        var commonSymptomPatterns = GetCommonSymptomPatterns();
        var commonBodyParts = GetCommonBodyParts();

        foreach (var (matchedKeyword, matchedIndex) in singleWordMatches.ToList())
        {
            var matchedKeywordLower = matchedKeyword.ToLowerInvariant();

            if (!commonSymptomPatterns.Contains(matchedKeywordLower))
            {
                continue;
            }

            if (TryUpgradeToPhrase(matchedKeyword, matchedKeywordLower, matchedIndex, lowerText, commonBodyParts, symptoms, foundKeywords, matchedPositions, singleWordMatches))
            {
                break;
            }
        }
    }

    private bool TryUpgradeToPhrase(
        string matchedKeyword,
        string matchedKeywordLower,
        int matchedIndex,
        string lowerText,
        string[] commonBodyParts,
        List<string> symptoms,
        HashSet<string> foundKeywords,
        Dictionary<string, int> matchedPositions,
        List<(string keyword, int index)> singleWordMatches)
    {
        foreach (var bodyPart in commonBodyParts)
        {
            var potentialPhrase = $"{matchedKeywordLower} {bodyPart}";

            if (!lowerText.Contains(potentialPhrase, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var allSymptoms = GetAllSymptomKeywords();
            var exists = allSymptoms.Any(k => k.Keyword.Equals(potentialPhrase, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                return UpgradeToExistingPhrase(matchedKeyword, potentialPhrase, matchedIndex, allSymptoms, symptoms, foundKeywords, matchedPositions, singleWordMatches);
            }
            else
            {
                return TryAddNewPhrase(matchedKeyword, potentialPhrase, matchedIndex, symptoms, foundKeywords, matchedPositions, singleWordMatches);
            }
        }

        return false;
    }

    private bool UpgradeToExistingPhrase(
        string matchedKeyword,
        string potentialPhrase,
        int matchedIndex,
        List<ConversationContextKeywordEntity> allSymptoms,
        List<string> symptoms,
        HashSet<string> foundKeywords,
        Dictionary<string, int> matchedPositions,
        List<(string keyword, int index)> singleWordMatches)
    {
        var existingKeyword = allSymptoms.FirstOrDefault(k =>
            k.Keyword.Equals(potentialPhrase, StringComparison.OrdinalIgnoreCase));

        if (existingKeyword == null || foundKeywords.Contains(potentialPhrase))
        {
            return false;
        }

        singleWordMatches.RemoveAll(m => m.keyword == matchedKeyword);
        symptoms.Add(existingKeyword.Keyword);
        foundKeywords.Add(potentialPhrase);
        matchedPositions[existingKeyword.Keyword] = matchedIndex;

        _logger.LogDebug("Upgraded '{Single}' to '{Phrase}'", matchedKeyword, potentialPhrase);
        return true;
    }

    private bool TryAddNewPhrase(
        string matchedKeyword,
        string potentialPhrase,
        int matchedIndex,
        List<string> symptoms,
        HashSet<string> foundKeywords,
        Dictionary<string, int> matchedPositions,
        List<(string keyword, int index)> singleWordMatches)
    {
        try
        {
            var newKeyword = CreateSymptomKeyword(potentialPhrase);
            _dbContext.ConversationContextKeywords.Add(newKeyword);
            _dbContext.SaveChanges();

            singleWordMatches.RemoveAll(m => m.keyword == matchedKeyword);
            symptoms.Add(potentialPhrase);
            foundKeywords.Add(potentialPhrase);
            matchedPositions[potentialPhrase] = matchedIndex;

            _logger.LogInformation(
                "Auto-added and upgraded '{Single}' to '{Phrase}' in ConversationContextKeywords",
                matchedKeyword, potentialPhrase);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-add symptom phrase '{Symptom}' to database", potentialPhrase);
            return false;
        }
    }

    private void AddRemainingSingleWordMatches(
        List<(string keyword, int index)> singleWordMatches,
        List<string> symptoms,
        HashSet<string> foundKeywords,
        Dictionary<string, int> matchedPositions)
    {
        foreach (var (keyword, index) in singleWordMatches)
        {
            var keywordLower = keyword.ToLowerInvariant();
            if (!foundKeywords.Contains(keywordLower))
            {
                symptoms.Add(keyword);
                foundKeywords.Add(keywordLower);
                matchedPositions[keyword] = index;
            }
        }
    }

    private void TryIntelligentExtraction(
        string lowerText,
        List<string> symptoms,
        HashSet<string> foundKeywords)
    {
        if (TryExtractHardcodedPatterns(lowerText, symptoms, foundKeywords))
        {
            return;
        }

        if (TryExtractIntelligentPhrases(lowerText, symptoms, foundKeywords))
        {
            return;
        }

        TryExtractSingleWords(lowerText, symptoms, foundKeywords);
    }

    private bool TryExtractHardcodedPatterns(
        string lowerText,
        List<string> symptoms,
        HashSet<string> foundKeywords)
    {
        var commonSymptomPatterns = GetCommonSymptomPatterns();
        var commonBodyParts = GetCommonBodyParts();

        foreach (var pattern in commonSymptomPatterns)
        {
            foreach (var bodyPart in commonBodyParts)
            {
                var phrase = $"{pattern} {bodyPart}";
                if (!lowerText.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var normalizedPhrase = phrase.Trim();
                if (TryAddOrUseExistingPhrase(normalizedPhrase, symptoms, foundKeywords))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryExtractIntelligentPhrases(
        string lowerText,
        List<string> symptoms,
        HashSet<string> foundKeywords)
    {
        var stopWords = new HashSet<string> { "tôi", "bị", "có", "bạn", "anh", "chị", "em", "ông", "bà" };
        var words = lowerText.Split(new[] { ' ', ',', '.', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !stopWords.Contains(w))
            .ToList();

        for (int i = 0; i < words.Count - 1; i++)
        {
            var word1 = words[i];
            var word2 = words[i + 1];
            var potentialPhrase = $"{word1} {word2}";

            if (!IsValidPotentialPhrase(potentialPhrase, lowerText, word1, word2))
            {
                continue;
            }

            if (TryAddOrUseExistingPhrase(potentialPhrase, symptoms, foundKeywords))
            {
                return true;
            }
        }

        return false;
    }

    private void TryExtractSingleWords(
        string lowerText,
        List<string> symptoms,
        HashSet<string> foundKeywords)
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
                TryAddSingleWordSymptom(normalizedSymptom, symptoms, foundKeywords);
            }
        }
    }

    private bool TryAddOrUseExistingPhrase(
        string phrase,
        List<string> symptoms,
        HashSet<string> foundKeywords)
    {
        var allSymptoms = GetAllSymptomKeywords();
        var exists = allSymptoms.Any(k => k.Keyword.Equals(phrase, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            var existingKeyword = allSymptoms.FirstOrDefault(k => k.Keyword.Equals(phrase, StringComparison.OrdinalIgnoreCase));
            if (existingKeyword != null)
            {
                symptoms.Add(existingKeyword.Keyword);
                foundKeywords.Add(existingKeyword.Keyword.ToLowerInvariant());
                return true;
            }
        }
        else if (!foundKeywords.Contains(phrase.ToLowerInvariant()))
        {
            return TryAddNewPhraseToDb(phrase, symptoms, foundKeywords);
        }

        return false;
    }

    private bool TryAddNewPhraseToDb(
        string phrase,
        List<string> symptoms,
        HashSet<string> foundKeywords)
    {
        try
        {
            var newKeyword = CreateSymptomKeyword(phrase);
            _dbContext.ConversationContextKeywords.Add(newKeyword);
            _dbContext.SaveChanges();

            symptoms.Add(phrase);
            foundKeywords.Add(phrase.ToLowerInvariant());

            _logger.LogInformation(
                "Auto-added new symptom phrase '{Symptom}' to ConversationContextKeywords",
                phrase);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to auto-add symptom phrase '{Symptom}' to database", phrase);
            return false;
        }
    }

    private void TryAddSingleWordSymptom(
        string normalizedSymptom,
        List<string> symptoms,
        HashSet<string> foundKeywords)
    {
        var allSymptoms = GetAllSymptomKeywords();
        var exists = allSymptoms.Any(k => k.Keyword.Equals(normalizedSymptom, StringComparison.OrdinalIgnoreCase));

        if (exists)
        {
            return;
        }

        try
        {
            var newKeyword = CreateSymptomKeyword(normalizedSymptom);
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

    private static bool IsValidPotentialPhrase(string potentialPhrase, string lowerText, string word1, string word2)
    {
        return lowerText.Contains(potentialPhrase, StringComparison.OrdinalIgnoreCase) &&
               word1.Length >= 3 &&
               word2.Length >= 3 &&
               !IsCommonWord(word1) &&
               !IsCommonWord(word2);
    }

    private List<ConversationContextKeywordEntity> GetAllSymptomKeywords()
    {
        return _dbContext.ConversationContextKeywords
            .Where(k => k.Category == "SYMPTOM")
            .ToList();
    }

    private static string[] GetCommonSymptomPatterns()
    {
        return new[] { "đau", "sưng", "tê", "ngứa", "nóng", "lạnh", "chảy", "ho", "sốt" };
    }

    private static string[] GetCommonBodyParts()
    {
        return new[] {
            "chân", "tay", "đầu", "bụng", "lưng", "ngực", "cổ", "mắt", "tai", "mũi", "họng",
            "răng", "lợi", "miệng", "hàm", "gót", "gối", "khuỷu", "vai", "cổ tay", "cổ chân",
            "thắt lưng", "vùng chậu", "ngón tay", "ngón chân", "bàn tay", "bàn chân"
        };
    }

    private static ConversationContextKeywordEntity CreateSymptomKeyword(string keyword)
    {
        return new ConversationContextKeywordEntity
        {
            Id = Guid.NewGuid(),
            Keyword = keyword,
            Category = "SYMPTOM",
            Synonyms = JsonSerializer.Serialize(new[] { keyword }, JsonOptions),
            CreatedAt = DateTime.UtcNow
        };
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
