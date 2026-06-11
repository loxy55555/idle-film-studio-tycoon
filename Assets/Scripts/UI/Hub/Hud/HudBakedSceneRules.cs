using UnityEngine;

/// <summary>Detects whether the scene already contains the baked definitive HUD (Phase 7A.3).</summary>
public static class HudBakedSceneRules
{
    public static bool IsBaked(Transform contentSwitcher)
    {
        if (contentSwitcher == null) return false;

        var shell = contentSwitcher.GetComponent<DefinitiveHudShell>();
        if (shell == null || shell.appliedMigrationVersion < DefinitiveHudShell.MigrationVersion)
            return false;

        if (shell.mainNavigation == null)
            return false;

        return HasConfiguredShells(contentSwitcher);
    }

    public static bool IsBakedScene()
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name != "ContentSwitcher") continue;
            return IsBaked(t);
        }

        return false;
    }

    static bool HasConfiguredShells(Transform contentSwitcher)
    {
        var estudio = FindPanel(contentSwitcher, "EstudioPanel");
        var production = FindPanel(contentSwitcher, "ProduccionPanel", "PeliculasPanel");
        var premios = FindPanel(contentSwitcher, "PremiosPanel");
        var collection = FindPanel(contentSwitcher, "ColeccionPanel");
        var menu = FindPanel(contentSwitcher, "MenuPanel", "TiendaPanel");

        if (estudio == null || production == null || premios == null || collection == null || menu == null)
            return false;

        if (estudio.GetComponent<StudioHudShell>()?.subNavigation == null)
            return false;
        if (production.GetComponent<ProductionHudShell>() == null)
            return false;
        if (premios.GetComponent<PremiosHudShell>()?.subNavigation == null)
            return false;
        if (collection.GetComponent<CollectionHudShell>()?.subNavigation == null)
            return false;
        if (menu.GetComponent<MenuHudShell>()?.settingsSection == null)
            return false;

        if (estudio.Find("StudioVisualStage") == null)
            return false;

        return true;
    }

    static Transform FindPanel(Transform parent, params string[] names)
    {
        foreach (var name in names)
        {
            var t = parent.Find(name);
            if (t != null) return t;
        }

        return null;
    }
}
