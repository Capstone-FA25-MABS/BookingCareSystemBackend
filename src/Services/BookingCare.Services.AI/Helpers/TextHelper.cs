using System.Text.RegularExpressions;

namespace BookingCare.Services.AI.Helpers;

/// <summary>
/// Helper class for text processing utilities
/// </summary>
public static class TextHelper
{
    /// <summary>
    /// Clean Vietnamese name - remove XML/HTML tags and unwanted text
    /// </summary>
    public static string CleanVietnameseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return name;

        // Remove XML/HTML tags like </think>, <think>, </think>, etc.
        var cleaned = Regex.Replace(
            name,
            @"</?[^>]+>",
            "",
            RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(2));

        // Remove common unwanted prefixes/suffixes (including </think> and </think>)
        // Note: Regex already removes all XML/HTML tags, but we also explicitly remove common ones
        cleaned = cleaned
            .Replace("</think>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<think>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("</think>", "", StringComparison.OrdinalIgnoreCase)
            .Replace("<think>", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return cleaned;
    }

    /// <summary>
    /// Extract Vietnamese text from mixed content using regex pattern
    /// </summary>
    public static string ExtractVietnameseText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // Try to find Vietnamese text (contains Vietnamese characters)
        var vietnamesePattern = @"[\u00C0-\u1EF9]+";
        var matches = System.Text.RegularExpressions.Regex.Matches(
            text,
            vietnamesePattern,
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(2));

        if (matches.Count > 0)
        {
            return string.Join(" ", matches.Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value));
        }

        return string.Empty;
    }
}

