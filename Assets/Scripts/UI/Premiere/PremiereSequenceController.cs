using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Orchestrates premiere presentation on movie completion (Phase 8.5).</summary>
public class PremiereSequenceController : MonoBehaviour
{
    public static PremiereSequenceController Instance { get; private set; }
    public static event Action OnPremiereDismissed;

    public static bool IsPresenting =>
        Instance != null && Instance._isPresenting;

    readonly Queue<MovieCompletePayload> _queue = new();
    PremiereOverlayView _overlay;
    bool _isPresenting;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void OnEnable() => GameHub.OnGameReady += EnsureOverlay;

    void OnDestroy()
    {
        GameHub.OnGameReady -= EnsureOverlay;
        if (Instance == this) Instance = null;
    }

    void EnsureOverlay()
    {
        if (_overlay != null) return;
        var canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;
        _overlay = PremiereOverlayView.Create(canvas.transform);
    }

    /// <summary>Returns true when premiere UI owns the completion moment.</summary>
    public static bool TryPresent(MovieCompletePayload payload)
    {
        if (payload.config == null && string.IsNullOrEmpty(payload.movieName))
            return false;

        EnsureInstance();
        if (Instance == null) return false;
        Instance.Enqueue(payload);
        return true;
    }

    static void EnsureInstance()
    {
        if (Instance != null) return;
        var hub = GameHub.Instance;
        if (hub == null) return;
        if (hub.GetComponent<PremiereSequenceController>() == null)
            hub.gameObject.AddComponent<PremiereSequenceController>();
    }

    void Enqueue(MovieCompletePayload payload)
    {
        _queue.Enqueue(payload);
        if (!_isPresenting)
            PresentNext();
    }

    void PresentNext()
    {
        if (_queue.Count == 0)
        {
            _isPresenting = false;
            return;
        }

        EnsureOverlay();
        if (_overlay == null)
        {
            _queue.Clear();
            _isPresenting = false;
            OnPremiereDismissed?.Invoke();
            return;
        }

        _isPresenting = true;
        var data = PremierePresentationData.From(_queue.Dequeue());
        _overlay.Play(data, OnPremiereClosed);
    }

    void OnPremiereClosed()
    {
        OnPremiereDismissed?.Invoke();
        PresentNext();
    }
}
