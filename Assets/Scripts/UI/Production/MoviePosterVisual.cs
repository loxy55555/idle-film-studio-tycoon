using UnityEngine;
using UnityEngine.UI;

/// <summary>Poster placeholder hook — sprite when available, color fallback otherwise.</summary>
public static class MoviePosterVisual
{
    public static MovieConfig FindByName(MovieConfig[] catalog, string movieName)
    {
        if (catalog == null || string.IsNullOrEmpty(movieName)) return null;
        foreach (var cfg in catalog)
        {
            if (cfg != null && cfg.movieName == movieName)
                return cfg;
        }
        return null;
    }

    public static MovieConfig[] ResolveCatalog() => MovieCatalogRuntime.AllMovies;

    public static void Apply(Image posterImage, MovieConfig config)
    {
        if (posterImage == null || config == null) return;
        Apply(posterImage, config.posterSprite, config.posterColorHex);
    }

    public static void Apply(Image posterImage, Sprite sprite, string colorHex)
    {
        if (posterImage == null) return;

        if (sprite != null)
        {
            posterImage.sprite = sprite;
            posterImage.color = Color.white;
            posterImage.preserveAspect = true;
            return;
        }

        posterImage.sprite = null;
        posterImage.preserveAspect = false;
        if (!string.IsNullOrEmpty(colorHex) && ColorUtility.TryParseHtmlString(colorHex, out Color c))
            posterImage.color = c;
        else
            posterImage.color = new Color(0.15f, 0.18f, 0.28f);
    }
}
