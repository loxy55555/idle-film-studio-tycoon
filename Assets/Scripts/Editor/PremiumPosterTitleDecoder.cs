#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Decodes concatenated poster filenames (e.g. ironwolves.png) into display titles.
/// FASE 16.5E: lowercase dictionary first, stem overrides, spacing normalization.
/// </summary>
public static class PremiumPosterTitleDecoder
{
    static readonly TextInfo Text = CultureInfo.InvariantCulture.TextInfo;
    static readonly string[] WordDictionary = BuildWordDictionary();
    static readonly Dictionary<string, string> StemOverrides = BuildStemOverrides();

    static readonly HashSet<string> RomanNumerals = new HashSet<string>
    {
        "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII",
    };

    public static string ToMovieName(string fileStem)
    {
        if (string.IsNullOrWhiteSpace(fileStem))
            return string.Empty;

        string stem = fileStem.Trim();
        if (stem.Contains(' ') || stem.Contains('-') || stem.Contains('_'))
            return NormalizeSpacing(TitleCase(NormalizeSeparators(stem)));

        string lowerKey = NormalizeStemKey(stem);
        if (StemOverrides.TryGetValue(lowerKey, out string overrideTitle))
            return overrideTitle;

        string dictResult = NormalizeSpacing(TitleCase(SplitLowercaseWords(lowerKey)));
        if (!LooksCorrupt(dictResult))
            return dictResult;

        string capResult = NormalizeSpacing(TitleCase(InsertSpacesBeforeCapitals(stem)));
        if (!LooksCorrupt(capResult))
            return capResult;

        return dictResult;
    }

    public static bool LooksCorrupt(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        if (Regex.IsMatch(name, @"  +"))
            return true;

        string[] words = name.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            string w = words[i];
            if (w.Length == 1 && char.IsLetter(w[0]) && char.IsUpper(w[0]))
            {
                if (w == "A" && i == 0) continue;
                if (RomanNumerals.Contains(w)) continue;
                return true;
            }
        }

        string lower = name.ToLowerInvariant();
        string[] badPhrases =
        {
            "ch a pel", "a z a", "projectl ", " projectl", "uncleg", "logest", "betweentwo",
            "whisperw", "gwitness", " thfloor", "oldpi", "lldis", "milyemergency",
            "departmentbelow", "reveryth", "gbroke", "frostpe ak", "goldenst ag",
            "gate s of", "bo a dc", "ye a ", "spl an", "of f ice", "record ing",
            "for esun", "utumnpromise", "onemore", "newneighbors",
            "miss in g", "dep a rt", "be for e ", "m on day", "n in th", "f a mily",
            "fam ily", "vill a ge", "gre at", "rchive", " a rchive",
        };
        foreach (string phrase in badPhrases)
        {
            if (lower.Contains(phrase))
                return true;
        }

        foreach (string w in words)
        {
            if (w.Length > 2 && Regex.IsMatch(w, @"[a-z][A-Z]"))
                return true;
        }

        if (Regex.IsMatch(name, @"\bOf\s+[A-Z]\b") || Regex.IsMatch(name, @"\bIn\s+[A-Z]\b"))
        {
            foreach (string w in words)
            {
                if (w.Length == 1)
                    return true;
            }
        }

        return false;
    }

    static string NormalizeStemKey(string stem) =>
        Regex.Replace(stem.ToLowerInvariant(), @"[^a-z0-9]", "");

    static string NormalizeSeparators(string value) =>
        value.Replace('_', ' ').Replace('-', ' ').Trim();

    static string NormalizeSpacing(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(value.Trim(), @"\s+", " ");

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

    static Dictionary<string, string> BuildStemOverrides()
    {
        var map = new Dictionary<string, string>();
        void Add(string stem, string title) => map[NormalizeStemKey(stem)] = title;

        Add("ProjectlAZARus", "Project Lazarus");
        Add("TheLogestSeaSOn", "The Longest Season");
        Add("UnclegARySplAn", "Uncle Gary's Plan");
        Add("TheYeAReverythInGbroke", "The Year Everything Broke");
        Add("TheLastDayOfF", "The Last Day Off");
        Add("AUtumnpromise", "Autumn Promise");
        Add("TheOldpiAnO", "The Old Piano");
        Add("WhisperwInDVALley", "Whisperwind Valley");
        Add("CASe114", "Case 114");
        Add("OnEmoreSummer", "One More Summer");
        Add("TheNewneighbors", "The New Neighbors");
        Add("TheLastBoADcASt", "The Last Broadcast");
        Add("TheDepARtmentbelow", "The Department Below");
        Add("TheWrOnGweddInG", "The Wrong Wedding");
        Add("TheLastDrAGOnKeeper", "The Last Dragon Keeper");
        Add("TheGoldenstAG", "The Golden Stag");
        Add("MoonSToNePath", "Moonstone Path");
        Add("HouseAtWinterLAKe", "House At Winter Lake");
        Add("TheBellbeneAtHTheLAKe", "The Bell Beneath The Lake");
        Add("BeneAtHAShHill", "Beneath Ash Hill");
        Add("BlackARchive", "Black Archive");
        Add("TheLastRecordInG", "The Last Recording");
        Add("TheWinterCAF", "The Winter Café");
        Add("DeadLetterOfFIce", "Dead Letter Office");

        return map;
    }

    static string[] BuildWordDictionary()
    {
        string[] words =
        {
            "operation", "extraction", "battalion", "frequency", "frontier", "dominion",
            "convoy", "kingdom", "empire", "ashes", "horizon", "contact", "crimson",
            "border", "beyond", "broken", "silent", "ghost", "ghosts", "steel", "wolves", "rain",
            "siege", "black", "night", "zero", "cold", "fire", "dust", "dead", "last",
            "iron", "the", "of", "and", "a", "an", "in", "on", "at", "to", "for", "from",
            "with", "without", "under", "over", "into", "out", "red", "blue", "dark",
            "light", "deep", "lost", "final", "first", "second", "third", "secret",
            "hidden", "fallen", "rising", "return", "legacy", "shadow", "blood",
            "heart", "soul", "mind", "echo", "echoes", "signal", "file", "line",
            "house", "river", "sky", "sea", "sun", "moon", "moons", "star", "stars", "world",
            "city", "road", "path", "gate", "gates", "wall", "war", "peace", "love", "life",
            "death", "time", "day", "days", "nightfall", "daybreak", "summer", "winter",
            "spring", "fall", "coldfire", "welcome", "disaster", "neighbors", "monday", "morning",
            "great", "mix", "mixup", "family", "emergency", "autumn", "harbor", "piano",
            "old", "between", "two", "seasons", "season", "longest", "bookshop", "cedar",
            "street", "emerald", "tower", "keeper", "forgotten", "mage", "whisper", "wind",
            "valley", "frost", "peak", "golden", "crystal", "forest", "village", "names",
            "promise", "cross", "paris", "midnight", "verona", "cafe", "letters", "ocean",
            "meets", "where", "europa", "array", "atlas", "paradox", "archive", "project",
            "lazarus", "evidence", "department", "witness", "miss", "missing", "ninth",
            "floor", "record", "recording", "chapel", "empty", "wedding", "home", "long",
            "book", "one", "more", "elderglen", "spellbound", "market", "thorns", "bargain",
            "shift", "grave", "parish", "moth", "bell", "beneath", "hill", "skinwalker",
            "accidental", "guru", "hero", "late", "again", "room", "error", "side", "gig",
            "uncle", "gary", "plan", "year", "everything", "broke", "pizza", "tribunal",
            "new", "double", "booked", "improv", "nation", "quiet", "paper", "hearts",
            "train", "delay", "case", "ice", "letter", "broadcast", "called", "tomorrow",
            "place", "sunrise", "before", "stone", "rain", "silent", "kingdom", "across",
            "hall", "rise", "off", "below", "office", "dragon", "stag", "moonstone",
            "lake", "ash", "wrong", "without", "name", "names", "train", "ris", "ver",
            "ona", "mid", "night", "verona", "dis", "ster", "hall", "disaster", "st",
            "ag", "sh", "ke", "dc", "ing", "ne", "path", "promise", "summer", "quiet",
        };

        return words
            .Distinct()
            .OrderByDescending(w => w.Length)
            .ThenBy(w => w, System.StringComparer.Ordinal)
            .ToArray();
    }
}
#endif
