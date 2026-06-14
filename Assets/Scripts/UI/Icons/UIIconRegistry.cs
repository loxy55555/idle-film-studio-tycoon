using UnityEngine;

/// <summary>Sprite registry for definitive UI icons under Assets/Art/UI/Icons/.</summary>
[CreateAssetMenu(fileName = "UIIconRegistry", menuName = "IdleFilm/UI Icon Registry")]
public class UIIconRegistry : ScriptableObject
{
    public const string ResourceName = "UIIconRegistry";

    [Header("Departments")]
    public Sprite deptEditor;
    public Sprite deptDirector;
    public Sprite deptActors;
    public Sprite deptSound;
    public Sprite deptCinematography;
    public Sprite deptMakeup;
    public Sprite deptCostume;
    public Sprite deptArt;
    public Sprite deptLighting;
    public Sprite deptGrip;
    public Sprite deptProducer;
    public Sprite deptMarketing;
    public Sprite deptScript;

    [Header("Genres")]
    public Sprite genreAction;
    public Sprite genreDrama;
    public Sprite genreHorror;
    public Sprite genreComedy;
    public Sprite genreRomance;
    public Sprite genreSciFi;
    public Sprite genreFantasy;
    public Sprite genreThriller;
    public Sprite genreAnimation;
    public Sprite genreDocumentary;

    [Header("Navigation")]
    public Sprite navStudio;
    public Sprite navProduction;
    public Sprite navAwards;
    public Sprite navCollection;
    public Sprite navShop;
    public Sprite navMissions;

    [Header("Resources")]
    public Sprite resMoney;
    public Sprite resReputation;
    public Sprite resGoldStar;
    public Sprite resTicket;
    public Sprite resExperience;
    public Sprite resDecoCamera;
    public Sprite resFilmReel;
    public Sprite resDiamonds;

    [Header("Awards")]
    public Sprite awardStar;
    public Sprite awardStarLocked;

    [Header("Utility")]
    public Sprite utilBoost;
    public Sprite utilSpeedProduction;
}
