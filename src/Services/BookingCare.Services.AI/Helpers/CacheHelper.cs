namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper class for cache-related utility methods
/// </summary>
public static class CacheHelper
{
    /// <summary>
    /// Find best fuzzy match using Jaccard similarity over comma-separated keywords.
    /// </summary>
    public static T? FindBestFuzzyMatch<T>(
        IEnumerable<T> candidates,
        Func<T, string> keywordSelector,
        List<string> keywordList,
        double threshold,
        Func<T, int>? usageSelector = null,
        Func<T, double>? successSelector = null)
    {
        var matches = candidates
            .Select(c => new
            {
                Item = c,
                Similarity = CalculateJaccardSimilarity(
                    keywordList,
                    keywordSelector(c)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(k => k.Trim())
                        .ToList())
            })
            .Where(m => m.Similarity >= threshold)
            .OrderByDescending(m => m.Similarity);

        if (usageSelector != null)
        {
            matches = matches.ThenByDescending(m => usageSelector(m.Item));
        }

        if (successSelector != null)
        {
            matches = matches.ThenByDescending(m => successSelector(m.Item));
        }

        var best = matches.FirstOrDefault();

        return best?.Item;
    }

    /// <summary>
    /// Calculate Jaccard similarity between two keyword sets
    /// Similarity = |intersection| / |union|
    /// </summary>
    public static double CalculateJaccardSimilarity(List<string> set1, List<string> set2)
    {
        if (set1.Count == 0 && set2.Count == 0)
            return 1.0;

        if (set1.Count == 0 || set2.Count == 0)
            return 0.0;

        var intersection = set1.Intersect(set2, StringComparer.OrdinalIgnoreCase).Count();
        var union = set1.Union(set2, StringComparer.OrdinalIgnoreCase).Count();

        return union > 0 ? (double)intersection / union : 0.0;
    }
}

