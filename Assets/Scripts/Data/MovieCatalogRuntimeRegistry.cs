using UnityEngine;

/// <summary>
/// Build/runtime registry of all MovieConfig assets (Phase 6.1).
/// Rebuilt from Assets/Data/Movies via IdleFilm → Catalog → Rebuild Scene Movie References.
/// </summary>
[CreateAssetMenu(menuName = "IdleFilm/Movie Catalog Runtime Registry", fileName = "MovieCatalogRuntimeRegistry")]
public class MovieCatalogRuntimeRegistry : ScriptableObject
{
    public const string ResourceName = "MovieCatalogRuntimeRegistry";
    public const string AssetPath = "Assets/Resources/MovieCatalogRuntimeRegistry.asset";

    public MovieConfig[] movies = System.Array.Empty<MovieConfig>();
}
