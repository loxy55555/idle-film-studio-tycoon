using UnityEngine;

public enum SagaSizeClass
{
    Single  = 1,
    Mini    = 2,
    Medium  = 4,
    Large   = 6,
}

[CreateAssetMenu(menuName = "IdleFilm/Saga")]
public class SagaDefinition : ScriptableObject
{
    [Header("Identity")]
    public string sagaId;
    public string displayName;
    [TextArea(1, 3)]
    public string description;

    [Header("Structure")]
    public SagaSizeClass sizeClass = SagaSizeClass.Medium;
    [Tooltip("Expected entries in this saga (1, 2, 4, 6…).")]
    public int expectedEntryCount = 4;

    [Header("Progression")]
    [Tooltip("City where the first entry typically unlocks.")]
    public int firstEntryCity = 1;

    public int ExpectedSize =>
        expectedEntryCount > 0 ? expectedEntryCount : (int)sizeClass;
}
