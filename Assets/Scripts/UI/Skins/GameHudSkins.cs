using UnityEngine;

/// <summary>Bundle of HUD skin assets assignable in the inspector or via Resources.</summary>
[CreateAssetMenu(fileName = "GameHudSkins", menuName = "IdleFilm/UI/Game HUD Skins")]
public class GameHudSkins : ScriptableObject
{
    public UIPanelSkin panelSkin;
    public UICardSkin cardSkin;
    public UIButtonSkin buttonSkin;
    public UITabSkin tabSkin;

    public static GameHudSkins CreateRuntimeDefaults()
    {
        var bundle = CreateInstance<GameHudSkins>();
        bundle.panelSkin = CreateInstance<UIPanelSkin>();
        bundle.cardSkin = CreateInstance<UICardSkin>();
        bundle.buttonSkin = CreateInstance<UIButtonSkin>();
        bundle.tabSkin = CreateInstance<UITabSkin>();
        return bundle;
    }
}
