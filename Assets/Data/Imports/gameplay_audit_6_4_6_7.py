#!/usr/bin/env python3
"""FASE 6.4–6.7 — Generate gameplay audit reports (static code verification)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
REPORTS = ROOT / "Assets" / "Data" / "Reports"

STUDIO = ROOT / "Assets/Scripts/Managers/StudioManager.cs"
COLLECTION_UI = ROOT / "Assets/Scripts/UI/Hub/MovieCollectionUI.cs"
CONTRACTS_UI = ROOT / "Assets/Scripts/UI/Hub/ContractsPanelUI.cs"
GAME_FEEL = ROOT / "Assets/Scripts/UI/Animation/GameFeelUI.cs"
LEGENDARY_CARD = ROOT / "Assets/Scripts/UI/Collection/CollectionMovieCardView.cs"
RARITY_VIS = ROOT / "Assets/Scripts/UI/Production/MovieRarityVisual.cs"
MOVIE_BTN = ROOT / "Assets/Scripts/UI/MovieButtonUI.cs"


def read(path):
    return path.read_text(encoding="utf-8") if path.exists() else ""


def verify_6_4():
    s = read(STUDIO)
    c = read(COLLECTION_UI)
    g = read(GAME_FEEL)
    checks = {
        "FinalizeProduction present": "void FinalizeProduction" in s,
        "OnMovieCompleted event": "OnMovieCompleted?.Invoke" in s,
        "MarkMovieCompleted": "MarkMovieCompleted" in s,
        "AddMoney on complete": "AddMoney(reward)" in s,
        "GameFeel Start fallback Bind": "if (GameHub.Instance != null) Bind()" in g,
        "Collection OnGameReady only": "GameHub.OnGameReady += Bind" in c and "void Start()" not in c,
    }
    return checks


def verify_6_5():
    c = read(CONTRACTS_UI)
    return {
        "Bind poison pattern": "GameHub.OnGameReady -= Bind" in c and "if (_contracts == null) return" in c,
        "OnEnable no Bind retry": "if (_contracts != null)" in c and "Rebuild()" in c,
        "Rebuild guard": "if (_contracts == null || activeContent == null) return" in c,
    }


def verify_6_6():
    card = read(LEGENDARY_CARD)
    btn = read(MOVIE_BTN)
    return {
        "Legendary real name in collection": "cfg.movieName" in card and "BuildLegendaryLabels" in card,
        "UnknownTitle for non-legendary": "UnknownTitle()" in card,
        "Production always shows name": "titleText.text = movieConfig.movieName" in btn,
    }


def verify_6_7():
    card = read(LEGENDARY_CARD)
    rv = read(RARITY_VIS)
    return {
        "Frame from rarity": "MovieRarityVisual.ApplyFrame" in card,
        "EpicGold defined": "EpicGold" in rv,
        "Legendary frame path": "ApplyLegendaryFrame" in card,
    }


def main():
    REPORTS.mkdir(parents=True, exist_ok=True)
    print("Static verification (reports written separately with runtime notes):")
    for k, v in verify_6_4().items():
        print(f"  6.4 {k}: {'PASS' if v else 'FAIL'}")
    for k, v in verify_6_5().items():
        print(f"  6.5 {k}: {'PASS' if v else 'FAIL'}")
    for k, v in verify_6_6().items():
        print(f"  6.6 {k}: {'PASS' if v else 'FAIL'}")
    for k, v in verify_6_7().items():
        print(f"  6.7 {k}: {'PASS' if v else 'FAIL'}")
    print(f"\nReports in {REPORTS.relative_to(ROOT)}/")


if __name__ == "__main__":
    main()
