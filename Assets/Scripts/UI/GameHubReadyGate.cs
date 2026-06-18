using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FASE 14.0B — Architectural fix for the "inactive panel misses OnGameReady" bug.
///
/// Root cause: UI panels start inactive → their Awake() fires only when the panel is first
/// opened → by then, GameHub.OnGameReady has already fired → the event subscription has
/// no effect → progress bars/data stay at default/empty until the user switches tabs.
///
/// Solution: replace bare `GameHub.OnGameReady += handler` calls in Awake() with
/// GameHubReadyGate.SubscribeOrInvokeNow(handler), which checks GameHub.Instance and
/// calls the handler immediately if the game is already initialized.
///
/// Usage pattern (in any MonoBehaviour):
///
///   void Awake()
///   {
///       GameHubReadyGate.SubscribeOrInvokeNow(Bind);   // covers late-activation case
///   }
///
///   void OnEnable()
///   {
///       GameHubReadyGate.RefreshIfReady(Bind);          // re-syncs on every panel re-open
///   }
///
///   void OnDestroy()
///   {
///       GameHub.OnGameReady -= Bind;                    // always unsubscribe in OnDestroy
///   }
/// </summary>
public static class GameHubReadyGate
{
    /// <summary>
    /// Replaces the two-line pattern:
    ///   GameHub.OnGameReady += handler;
    ///   if (GameHub.Instance != null) handler();
    ///
    /// If GameHub is already initialized, handler is invoked immediately.
    /// Otherwise it is registered for the upcoming OnGameReady event.
    ///
    /// The caller is still responsible for unsubscribing via
    /// GameHub.OnGameReady -= handler in OnDestroy().
    /// </summary>
    public static void SubscribeOrInvokeNow(Action handler)
    {
        if (handler == null) return;

        if (GameHub.Instance != null)
            handler.Invoke();
        else
            GameHub.OnGameReady += handler;
    }

    /// <summary>
    /// Call in OnEnable() to re-sync the component whenever its panel becomes visible.
    /// Only invokes handler if GameHub is already ready (safe no-op otherwise).
    /// Optionally rebuilds a Slider's layout to fix the common Unity rendering issue
    /// where the fill rect is invisible on the first frame a panel becomes active.
    /// </summary>
    public static void RefreshIfReady(Action handler, RectTransform sliderParent = null)
    {
        if (GameHub.Instance == null) return;

        handler?.Invoke();

        if (sliderParent != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(sliderParent);
    }
}
