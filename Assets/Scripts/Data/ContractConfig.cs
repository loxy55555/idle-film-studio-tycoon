using UnityEngine;

public enum ContractGoalType
{
    ProduceMovies,          // produce N movies total
    ProduceMoviesByGenre,   // produce N movies of a genre
    ReachReputation,        // reach REP threshold
    ReachQuality,           // produce a movie with quality >= N
    EarnMoney,              // earn N$ total
    SpendOnUpgrades,        // spend N$ on any upgrade
    ReachStudioLevel,       // reach studio level N
    ProduceMoviesUnderTime, // produce a movie in <= N seconds
}

[CreateAssetMenu(menuName = "IdleFilm/Contract")]
public class ContractConfig : ScriptableObject
{
    [Header("Identity")]
    public string id;
    public string contractTitle;
    [TextArea(1, 3)]
    public string description;

    [Header("Goal")]
    public ContractGoalType goalType;
    public MovieGenre targetGenre;  // only for ProduceMoviesByGenre
    public float goalAmount;        // meaning depends on goalType
    public float timeLimit;         // seconds; 0 = no time limit

    [Header("Rewards")]
    public long    rewardMoney;
    public int     rewardDiamonds;
    public float   rewardReputation;
    public int     rewardStudioXP;

    [Header("Unlock")]
    public int unlockStudioLevel = 0;

    [Tooltip("Minimum city level required. 1 = Garaje.")]
    public int unlockCityLevel = 1;

    [Tooltip("Contract difficulty band for city gating.")]
    public ContractDifficultyBand difficultyBand = ContractDifficultyBand.Simple;

    [Header("Recurrence")]
    [Tooltip("Can this contract reappear after completion?")]
    public bool repeatable = false;
}
