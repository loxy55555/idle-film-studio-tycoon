using UnityEngine;
using UnityEngine.UI;

/// <summary>Makes progress sliders display-only (no drag / selection).</summary>
public static class ReadOnlySlider
{
    public static void Configure(Slider slider)
    {
        if (slider == null) return;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
    }
}
