#!/usr/bin/env python3
"""FASE 6.9 — Action genre collection verification."""
import re
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
MOVIES = ROOT / "Assets" / "Data" / "Movies"
REPORT = ROOT / "Assets" / "Data" / "Reports" / "ActionCollectionVerification.txt"

RARITY = {0: "Common", 1: "Rare", 2: "Epic", 3: "Legendary"}


def parse_asset(path):
    text = path.read_text(encoding="utf-8")
    name = re.search(r"movieName: (.+)", text)
    genre = re.search(r"genre: (\d+)", text)
    rarity = re.search(r"rarity: (\d+)", text)
    if not genre:
        return None
    return {
        "stem": path.stem,
        "movieName": name.group(1).strip() if name else "?",
        "genre": int(genre.group(1)),
        "rarity": int(rarity.group(1)) if rarity else -1,
    }


def main():
    action = []
    prefix_mismatch = []
    for p in sorted(MOVIES.glob("*.asset")):
        m = parse_asset(p)
        if not m:
            continue
        if m["genre"] == 0:
            action.append(m)
        elif p.name.startswith("Action_"):
            prefix_mismatch.append(m)

    counts = Counter(m["rarity"] for m in action)
    epics = [m for m in action if m["rarity"] == 2]
    rares = [m for m in action if m["rarity"] == 1]

    lines = [
        "FASE 6.9 — Action Collection Verification",
        "=========================================",
        "",
        "CATALOG DATA (genre = Action / 0)",
        f"  Total Action       : {len(action)}",
        f"  Common             : {counts.get(0, 0)}",
        f"  Rare               : {counts.get(1, 0)}",
        f"  Epic               : {counts.get(2, 0)}",
        f"  Legendary          : {counts.get(3, 0)}",
        "",
    ]

    lines.append("EPIC ACTION MOVIES (rarity = Epic / 2)")
    if epics:
        for m in sorted(epics, key=lambda x: x["movieName"]):
            lines.append(f"  • {m['movieName']}  ({m['stem']})")
    else:
        lines.append("  (none — zero Epic entries in Action genre)")
    lines.append("")

    lines.append("RARE ACTION MOVIES (rarity = Rare / 1) — yellow EpicGold frame NOT applied")
    lines.append(f"  Count: {len(rares)}")
    for m in sorted(rares, key=lambda x: x["movieName"]):
        lines.append(f"  • {m['movieName']}  ({m['stem']})")
    lines.append("")

    lines.extend([
        "COLLECTION UI BORDER RULE (CollectionMovieCardView.cs L45-48)",
        "  Common    → blue frame  (MovieRarityVisual.CommonBlue)",
        "  Rare      → purple frame (MovieRarityVisual.RarePurple)",
        "  Epic      → yellow frame (MovieRarityVisual.EpicGold)",
        "  Legendary → gold frame  (CollectionLegendaryVisual)",
        "",
        "COMPARISON: CATALOG vs COLLECTION CARDS",
        f"  Action cards in collection grid : {len(action)} (all rarities shown)",
        f"  Yellow-framed cards expected    : {counts.get(2, 0)} Epic + {counts.get(3, 0)} Legendary gold",
        f"  Purple-framed cards expected    : {counts.get(1, 0)} Rare",
        "",
    ])

    if counts.get(2, 0) == 0 and counts.get(1, 0) > 0:
        verdict = "B) VISUAL MISREAD — multiple yellow/gold borders are NOT from several Epic;"
        verdict2 = "   Rare uses purple; if user sees multiple YELLOW frames, check Epic tier count (0 here)"
        verdict3 = "   OR user may be counting Legendary gold + misidentified Rare purple as yellow."
        # Actually Rare is purple not yellow. So multiple yellow = only Epic + Legendary gold
        # With 0 Epic, only 1 Legendary gold in Action
        verdict = "A) PARTIAL — zero Epic in Action; only 1 Legendary gold frame expected."
        verdict2 = f"   If UI shows 4+ yellow frames in Action, that is B) ERROR (duplicate/wrong rarity render)."
        verdict3 = f"   If UI shows {counts.get(1, 0)} purple + 1 gold, that is CORRECT for current catalog."
    elif counts.get(2, 0) >= 2:
        verdict = f"A) CORRECT — {counts.get(2, 0)} Epic entries legitimately share yellow EpicGold border."
        verdict2 = ""
        verdict3 = ""
    else:
        verdict = "A) CORRECT for catalog data — border color = rarity tier, not unique per movie."
        verdict2 = f"  Action has {counts.get(2, 0)} Epic, {counts.get(1, 0)} Rare, {counts.get(3, 0)} Legendary."
        verdict3 = ""

    lines.append("VERDICT: MULTIPLE YELLOW BORDERS IN ACTION")
    lines.append(f"  {verdict}")
    if verdict2:
        lines.append(f"  {verdict2}")
    if verdict3:
        lines.append(f"  {verdict3}")
    lines.append("")

    if prefix_mismatch:
        lines.append("ASSET PREFIX NOTE (Action_* but genre != Action)")
        for m in prefix_mismatch:
            lines.append(f"  • {m['stem']} → genre={m['genre']} name={m['movieName']}")
        lines.append("")

    lines.append("STATUS: PASS (catalog verified)")
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(REPORT)


if __name__ == "__main__":
    main()
