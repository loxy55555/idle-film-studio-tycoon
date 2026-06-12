using System;
using UnityEngine;

/// <summary>Approved definitive catalog layout — 300 movies, 10 genres, 10 legendaries (Phase CATÁLOGO 2).</summary>
public static class MovieCatalogSchema
{
    public const int SchemaVersion = 4;
    public const int TargetMovieCount = ContentScaleDatabase.TargetMovieCount;
    public const int MoviesPerGenre = 30;
    public const int GenreCount = 10;
    public const int LegendaryCount = 10;
    public const int LegendariesPerGenre = 1;

    /// <summary>Per-genre movie totals for the definitive 300-movie catalog.</summary>
    public static readonly int[] DefinitiveGenreCounts = { 45, 40, 35, 35, 35, 30, 25, 25, 20, 10 };

    public static int TargetCountForGenre(MovieGenre genre)
    {
        int idx = Array.IndexOf(DefinitiveGenreOrder, genre);
        return idx >= 0 ? DefinitiveGenreCounts[idx] : 0;
    }

    /// <summary>Official genre order for the master catalog.</summary>
    public static readonly MovieGenre[] DefinitiveGenreOrder =
    {
        MovieGenre.Action,
        MovieGenre.SciFi,
        MovieGenre.Drama,
        MovieGenre.Horror,
        MovieGenre.Comedy,
        MovieGenre.Fantasy,
        MovieGenre.Romance,
        MovieGenre.Thriller,
        MovieGenre.Animation,
        MovieGenre.Documentary,
    };

    public static readonly MovieRarity[] RarityOrder =
    {
        MovieRarity.Common,
        MovieRarity.Rare,
        MovieRarity.Epic,
        MovieRarity.Legendary,
    };

    /// <summary>Genres that existed before CATÁLOGO 2 (180-movie base).</summary>
    public static readonly MovieGenre[] LegacyGenres =
    {
        MovieGenre.Action,
        MovieGenre.SciFi,
        MovieGenre.Drama,
        MovieGenre.Horror,
        MovieGenre.Comedy,
        MovieGenre.Romance,
    };

    /// <summary>New genres introduced in CATÁLOGO 2 (120 slots).</summary>
    public static readonly MovieGenre[] ExpansionGenres =
    {
        MovieGenre.Fantasy,
        MovieGenre.Thriller,
        MovieGenre.Animation,
        MovieGenre.Documentary,
    };

    public static bool IsApprovedGenre(MovieGenre genre) =>
        Array.IndexOf(DefinitiveGenreOrder, genre) >= 0;

    public static bool IsExpansionGenre(MovieGenre genre) =>
        Array.IndexOf(ExpansionGenres, genre) >= 0;

    public static string GenreAssetPrefix(MovieGenre genre) => genre switch
    {
        MovieGenre.Action     => "Action",
        MovieGenre.SciFi      => "SciFi",
        MovieGenre.Drama      => "Drama",
        MovieGenre.Horror     => "Horror",
        MovieGenre.Comedy     => "Comedy",
        MovieGenre.Romance    => "Romance",
        MovieGenre.Fantasy    => "Fantasy",
        MovieGenre.Thriller   => "Thriller",
        MovieGenre.Animation  => "Animation",
        MovieGenre.Documentary => "Documentary",
        _                     => genre.ToString(),
    };

    public static string DefaultPosterHex(MovieGenre genre) => genre switch
    {
        MovieGenre.Action      => "#E74C3C",
        MovieGenre.SciFi       => "#1ABC9C",
        MovieGenre.Drama       => "#3498DB",
        MovieGenre.Horror      => "#8E44AD",
        MovieGenre.Comedy      => "#F39C12",
        MovieGenre.Fantasy     => "#9B59B6",
        MovieGenre.Romance     => "#E91E63",
        MovieGenre.Thriller    => "#34495E",
        MovieGenre.Animation   => "#E67E22",
        MovieGenre.Documentary => "#607D8B",
        _                      => "#3498DB",
    };

    public static string SlotCatalogId(MovieGenre genre, int slotIndex)
    {
        string prefix = GenreAssetPrefix(genre);
        if (slotIndex == MoviesPerGenre)
            return $"{prefix}_Legendary";
        return $"{prefix}_Slot_{slotIndex:D2}";
    }

    public static string SlotDisplayName(MovieGenre genre, int slotIndex, bool legendary)
    {
        string label = GenreAssetPrefix(genre);
        if (legendary)
            return $"[TBD Legendary {label}]";
        return $"[TBD {label} {slotIndex:D2}]";
    }

    public static bool TryParseGenre(string text, out MovieGenre genre)
    {
        genre = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        return Enum.TryParse(text.Trim(), true, out genre) && IsApprovedGenre(genre);
    }

    public static bool TryParseRarity(string text, out MovieRarity rarity)
    {
        rarity = MovieRarity.Common;
        if (string.IsNullOrWhiteSpace(text)) return true;
        return Enum.TryParse(text.Trim(), true, out rarity);
    }
}
