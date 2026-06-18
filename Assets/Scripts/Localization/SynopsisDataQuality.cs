using System;

/// <summary>
/// Detects Spanish synopsis fields that actually contain English/tagline copy.
/// FASE 16.4 — prevents mixed-language collection text at runtime.
/// </summary>
public static class SynopsisDataQuality
{
    public static bool IsMislabeledSpanish(string synopsis, MovieConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(synopsis) || cfg == null) return false;

        var s = synopsis.Trim();
        if (!string.IsNullOrWhiteSpace(cfg.synopsisEn)
            && string.Equals(s, cfg.synopsisEn.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(cfg.tagline)
            && string.Equals(s, cfg.tagline.Trim(), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public static string ResolveSpanish(MovieConfig cfg)
    {
        if (cfg == null) return null;
        if (!string.IsNullOrEmpty(cfg.synopsis) && !IsMislabeledSpanish(cfg.synopsis, cfg))
            return cfg.synopsis;
        return SynopsisRuntime.GenerateSpanish(cfg);
    }
}
