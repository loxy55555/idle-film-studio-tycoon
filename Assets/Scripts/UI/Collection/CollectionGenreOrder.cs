/// <summary>Official collection genre display order (Phase 8.6 / 9.0).</summary>
public static class CollectionGenreOrder
{
    public static MovieGenre[] DisplayOrder => MovieCatalogSchema.DefinitiveGenreOrder;

    public static MovieRarity[] RarityOrder => MovieCatalogSchema.RarityOrder;
}
