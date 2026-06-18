using UnityEngine;

/// <summary>Build-time index of audio clips under Assets/Audio (populated by editor builder).</summary>
[CreateAssetMenu(fileName = "AudioClipIndex", menuName = "IdleFilm/Audio Clip Index")]
public class AudioClipIndex : ScriptableObject
{
    public AudioClip[] clips;
}
