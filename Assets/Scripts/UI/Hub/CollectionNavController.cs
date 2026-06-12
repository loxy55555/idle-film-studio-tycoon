using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages 3-level collection navigation: L1 Genres → L2 Movie Grid → L3 Popup.
/// Placed on the ColeccionPanel root; wires GenreSelectionUI to MovieCollectionUI.
/// No gameplay logic — navigation only.
/// </summary>
public class CollectionNavController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject genrePanel;
    public GameObject moviePanel;
    public GameObject backBarGo;

    [Header("Back bar")]
    public TextMeshProUGUI genreTitleText;
    public Button backButton;

    GenreSelectionUI _genreUI;
    MovieCollectionUI _collectionUI;
    bool _wired;

    void Awake()
    {
        EnsureWired();
    }

    void OnEnable()
    {
        EnsureWired();
        ShowGenres();
    }

    void EnsureWired()
    {
        if (_wired) return;

        if (genrePanel != null)
            _genreUI = genrePanel.GetComponentInChildren<GenreSelectionUI>(true);

        if (moviePanel != null)
            _collectionUI = moviePanel.GetComponentInChildren<MovieCollectionUI>(true);

        if (_genreUI != null)
            _genreUI.OnGenreSelected = OnGenreChosen;

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowGenres);
            backButton.onClick.AddListener(ShowGenres);
        }

        _wired = true;
    }

    public void OnGenreChosen(MovieGenre? genre)
    {
        if (genre == null)
        {
            ShowGenres();
            return;
        }

        ShowMovies(genre.Value);
    }

    public void ShowGenres()
    {
        if (genrePanel != null) genrePanel.SetActive(true);
        if (moviePanel  != null) moviePanel.SetActive(false);
        if (backBarGo   != null) backBarGo.SetActive(false);

        if (_genreUI != null)
            _genreUI.RefreshProgress();
    }

    public void ShowMovies(MovieGenre genre)
    {
        if (genrePanel != null) genrePanel.SetActive(false);
        if (moviePanel  != null) moviePanel.SetActive(true);
        if (backBarGo   != null) backBarGo.SetActive(true);

        if (genreTitleText != null)
            genreTitleText.text = Loc.Get(GenreLocKey(genre));

        if (_collectionUI != null)
        {
            _collectionUI.FilterGenre = genre;
            _collectionUI.Refresh();
        }
    }

    static string GenreLocKey(MovieGenre g) => g switch
    {
        MovieGenre.Action      => LocKeys.GenreAction,
        MovieGenre.Drama       => LocKeys.GenreDrama,
        MovieGenre.Horror      => LocKeys.GenreHorror,
        MovieGenre.Comedy      => LocKeys.GenreComedy,
        MovieGenre.Romance     => LocKeys.GenreRomance,
        MovieGenre.SciFi       => LocKeys.GenreSciFi,
        MovieGenre.Fantasy     => LocKeys.GenreFantasy,
        MovieGenre.Thriller    => LocKeys.GenreThriller,
        MovieGenre.Animation   => LocKeys.GenreAnimation,
        MovieGenre.Documentary => LocKeys.GenreDocumentary,
        _                      => LocKeys.GenreAction,
    };
}
