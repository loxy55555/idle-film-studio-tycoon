using UnityEngine;

/// <summary>Assigns GameHudSkins to HudSkinProvider at runtime (HubRoot).</summary>
public class HudSkinBootstrap : MonoBehaviour
{
    [SerializeField] GameHudSkins skins;

    void Awake()
    {
        if (skins != null)
            HudSkinProvider.SetActive(skins);
        else
            HudSkinProvider.EnsureLoaded();
    }
}
