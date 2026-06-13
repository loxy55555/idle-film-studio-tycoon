using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Orchestrates premiere presentation on player discovery (Phase 10.3).</summary>
public class PremiereSequenceController : MonoBehaviour
{
    struct QueuedPremiere
    {
        public MovieCompletePayload payload;
        public Action onDismissed;
    }

    public static PremiereSequenceController Instance { get; private set; }
    public static event Action OnPremiereDismissed;

    public static bool IsPresenting =>
        Instance != null && Instance._isPresenting;

    readonly Queue<QueuedPremiere> _queue = new();
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

    public static bool TryPresent(MovieCompletePayload payload, Action onDismissed = null)
    {
        if (payload.config == null && string.IsNullOrEmpty(payload.movieName))
            return false;

        EnsureInstance();
        if (Instance == null) return false;
        Instance.Enqueue(payload, onDismissed);
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

    void Enqueue(MovieCompletePayload payload, Action onDismissed)
    {
        _queue.Enqueue(new QueuedPremiere { payload = payload, onDismissed = onDismissed });
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
            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();
                item.onDismissed?.Invoke();
            }
            _isPresenting = false;
            OnPremiereDismissed?.Invoke();
            return;
        }

        _isPresenting = true;
        var next = _queue.Dequeue();
        var data = PremierePresentationData.From(next.payload);
        _overlay.Play(data, () => OnPremiereClosed(next.onDismissed));
    }

    void OnPremiereClosed(Action onDismissed)
    {
        onDismissed?.Invoke();
        OnPremiereDismissed?.Invoke();
        PresentNext();
    }
}
