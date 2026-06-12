#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Decodes concatenated poster filenames (e.g. ironwolves.png) into display titles.
/// Inverse of catalog poster_file slugging — no new names invented beyond filename content.
/// </summary>
public static class PremiumPosterTitleDecoder
{
    static readonly TextInfo Text = CultureInfo.InvariantCulture.TextInfo;
    static readonly string[] WordDictionary = BuildWordDictionary();

    public static string ToMovieName(string fileStem)
    {
        if (string.IsNullOrWhiteSpace(fileStem))
            return string.Empty;

        string stem = fileStem.Trim();
        if (stem.Contains(' ') || stem.Contains('-') || stem.Contains('_'))
            return TitleCase(NormalizeSeparators(stem));

        if (Regex.IsMatch(stem, "[A-Z]"))
            return TitleCase(InsertSpacesBeforeCapitals(stem));

        return TitleCase(SplitLowercaseWords(stem.ToLowerInvariant()));
    }

    static string NormalizeSeparators(string value) =>
        value.Replace('_', ' ').Replace('-', ' ').Trim();

    static string InsertSpacesBeforeCapitals(string value)
    {
        string spaced = Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
        spaced = Regex.Replace(spaced, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return spaced;
    }

    static string SplitLowercaseWords(string lower)
    {
        if (string.IsNullOrEmpty(lower))
            return string.Empty;

        var words = new List<string>();
        int index = 0;
        while (index < lower.Length)
        {
            string match = FindLongestWord(lower, index);
            if (string.IsNullOrEmpty(match))
            {
                int next = index + 1;
                while (next < lower.Length && FindLongestWord(lower, next) == null)
                    next++;
                words.Add(lower.Substring(index, next - index));
                index = next;
                continue;
            }

            words.Add(match);
            index += match.Length;
        }

        return string.Join(" ", words);
    }

    static string FindLongestWord(string text, int start)
    {
        string best = null;
        foreach (string word in WordDictionary)
        {
            if (start + word.Length > text.Length) continue;
            if (text.Substring(start, word.Length) != word) continue;
            if (best == null || word.Length > best.Length)
                best = word;
        }

        return best;
    }

    static string TitleCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var parts = value.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            parts[i] = Text.ToTitleCase(parts[i].ToLowerInvariant());
        }

        return string.Join(" ", parts.Where(p => p.Length > 0));
    }

    static string[] BuildWordDictionary()
    {
        string[] words =
        {
            "operation", "extraction", "battalion", "frequency", "frontier", "dominion",
            "convoy", "kingdom", "empire", "ashes", "horizon", "contact", "crimson",
            "border", "beyond", "broken", "silent", "ghost", "steel", "wolves", "rain",
            "siege", "black", "night", "zero", "cold", "fire", "dust", "dead", "last",
            "iron", "the", "of", "and", "a", "an", "in", "on", "at", "to", "for", "from",
            "with", "without", "under", "over", "into", "out", "red", "blue", "dark",
            "light", "deep", "lost", "final", "first", "second", "third", "secret",
            "hidden", "fallen", "rising", "return", "legacy", "shadow", "blood",
            "heart", "soul", "mind", "echo", "echoes", "signal", "file", "line",
            "house", "river", "sky", "sea", "sun", "moon", "star", "stars", "world",
            "city", "road", "path", "gate", "wall", "war", "peace", "love", "life",
            "death", "time", "day", "days", "nightfall", "daybreak", "summer", "winter",
            "spring", "fall", "coldfire",
        };

        return words
            .Distinct()
            .OrderByDescending(w => w.Length)
            .ThenBy(w => w, System.StringComparer.Ordinal)
            .ToArray();
    }
}
#endif
