using UnityEngine;

/// <summary>Decorative production icons — no posters (Phase 10.2).</summary>
public static class ProductionDecorIcon
{
    public static Sprite GetSprite(MovieRarity rarity, MovieGenre genre = MovieGenre.Drama) =>
        UIIconCatalog.GetProductionIcon(rarity, genre);

    public static string GetIcon(MovieRarity rarity, MovieGenre genre = MovieGenre.Drama)
    {
        return rarity switch
        {
            MovieRarity.Legendary => "🏆",
            MovieRarity.Epic      => "🎥",
            MovieRarity.Rare      => "📽️",
            _                     => GetGenreIcon(genre),
        };
    }

    public static Color GetIconColor(MovieRarity rarity, MovieGenre genre = MovieGenre.Drama)
    {
        return rarity switch
        {
            MovieRarity.Legendary => new Color(1f, 0.88f, 0.35f),
            MovieRarity.Epic      => new Color(0.92f, 0.55f, 1f),
            MovieRarity.Rare      => new Color(0.55f, 0.82f, 1f),
            _                     => GetGenreColor(genre),
        };
    }

    static string GetGenreIcon(MovieGenre genre) => genre switch
    {
        MovieGenre.Action      => "🎬",
        MovieGenre.Drama       => "🎭",
        MovieGenre.Horror      => "👻",
        MovieGenre.Comedy      => "😂",
        MovieGenre.Romance     => "💕",
        MovieGenre.SciFi       => "🚀",
        MovieGenre.Fantasy     => "🐉",
        MovieGenre.Thriller    => "🔪",
        MovieGenre.Animation   => "✨",
        MovieGenre.Documentary => "📹",
        _                      => "🎬",
    };

    static Color GetGenreColor(MovieGenre genre) => genre switch
    {
        MovieGenre.Action      => new Color(0.95f, 0.45f, 0.40f),
        MovieGenre.Drama       => new Color(0.55f, 0.75f, 0.95f),
        MovieGenre.Horror      => new Color(0.75f, 0.45f, 0.90f),
        MovieGenre.Comedy      => new Color(0.98f, 0.78f, 0.25f),
        MovieGenre.Romance     => new Color(0.95f, 0.45f, 0.65f),
        MovieGenre.SciFi       => new Color(0.45f, 0.85f, 0.65f),
        MovieGenre.Fantasy     => new Color(0.70f, 0.50f, 0.95f),
        MovieGenre.Thriller    => new Color(0.90f, 0.55f, 0.25f),
        MovieGenre.Animation   => new Color(0.45f, 0.90f, 0.85f),
        MovieGenre.Documentary => new Color(0.65f, 0.70f, 0.78f),
        _                      => new Color(0.72f, 0.78f, 0.92f),
    };
}
