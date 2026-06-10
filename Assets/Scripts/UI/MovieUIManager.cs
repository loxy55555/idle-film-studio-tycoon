using System.Collections.Generic;
using UnityEngine;

public class MovieUIManager : MonoBehaviour
{
    public StudioManager studio;
    public Transform container;
    public GameObject buttonPrefab;
    public List<MovieConfig> movies;

    private void Start()
    {
        // Skip if references not assigned (static UI built by StudioUIBuilder is used instead)
        if (container == null || buttonPrefab == null) return;
        // If static cards already exist, skip dynamic generation
        if (container.childCount > 0) return;
        GenerateUI();
    }

    public void GenerateUI()
    {
        if (container == null || buttonPrefab == null)
        {
            Debug.LogError("MovieUIManager: container o buttonPrefab no asignados.");
            return;
        }

        ClearContainer();

        foreach (MovieConfig movie in movies)
        {
            GameObject obj = Instantiate(buttonPrefab, container);
            MovieButtonUI ui = obj.GetComponent<MovieButtonUI>();

            if (ui == null)
                ui = obj.AddComponent<MovieButtonUI>();

            ui.Setup(movie, studio);
        }
    }

    private void ClearContainer()
    {
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}
