#!/usr/bin/env python3
"""CATÁLOGO 5.0 — Integrate 130 premium posters into definitive 300 catalog."""
import csv
import os
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
POSTERS_ROOT = ROOT / "Assets" / "Posters"
MASTER_IN = ROOT / "Assets" / "Data" / "Imports" / "MovieCatalog_Definitive_300.csv"
PREMIUM_NAMES = ROOT / "Assets" / "Data" / "Exports" / "PremiumPosterCatalog.csv"
MASTER_OUT = ROOT / "Assets" / "Data" / "Imports" / "MovieCatalog_Integrated_300.csv"
PREMIUM_OUT = ROOT / "Assets" / "Data" / "Exports" / "PremiumCatalogFinal.csv"
COMMON_OUT = ROOT / "Assets" / "Data" / "Exports" / "CommonCatalogFinal.csv"
SUMMARY_OUT = ROOT / "Assets" / "Data" / "Reports" / "Catalog5_0_IntegrationSummary.txt"

FOLDER_GENRE = {
    "accion": "Action",
    "action": "Action",
    "sci-fi": "SciFi",
    "scifi": "SciFi",
    "drama": "Drama",
    "terror": "Horror",
    "horror": "Horror",
    "comedia": "Comedy",
    "comedy": "Comedy",
    "fantasia": "Fantasy",
    "fantasy": "Fantasy",
    "romance": "Romance",
    "thriller": "Thriller",
    "animacion": "Animation",
    "animación": "Animation",
    "animation": "Animation",
    "documental": "Documentary",
    "documentary": "Documentary",
}

LEGENDARIES = {
    "Action": ("Action_Legendary", "Iron Wolves"),
    "SciFi": ("SciFi_Legendary", "Echoes of Titan"),
    "Drama": ("Drama_Legendary", "When the River Sleeps"),
    "Horror": ("Horror_Legendary", "The Hollow House"),
    "Comedy": ("Comedy_Legendary", "Detective Potato"),
    "Fantasy": ("Fantasy_Legendary", "Song of the Forgotten Realm"),
    "Romance": ("Romance_Legendary", "A Sky for Two"),
    "Thriller": ("Thriller_Legendary", "Red File 27"),
    "Animation": ("Animation_Legendary", "Little Giants"),
    "Documentary": ("Documentary_Legendary", "Voices of the Deep"),
}

WORDS = sorted(
    set(
        """operation extraction battalion frequency frontier dominion convoy kingdom empire ashes
        horizon contact crimson border beyond broken silent ghost steel wolves rain siege black night
        zero cold fire dust dead last iron the of and a an in on at to for from with under over into
        out red blue dark light deep lost final first second third secret hidden fallen rising return
        legacy shadow blood heart soul mind echo echoes signal file line house river sky sea sun moon
        star stars world city road path gate wall war peace love life death time day summer winter
        coldfire voice voices deep titan potato giants hollow river sleeps forgotten realm file27
        moonlight bakery ocean maps captain acorn fireflies thousand treehouse orchard lantern parade
        sky whale volcano reef forest desert cities tomorrow ice hidden wild maple hill clockwork
        fox little documentary beyond silent orbit drift final empty colony galaxy paradox beacon
        aether collapse matter protocol helios europa station eos whispering woods watcher motel
        """.split()
    ),
    key=len,
    reverse=True,
)

HEADER = [
    "catalogId",
    "movieName",
    "genre",
    "rarity",
    "cityUnlock",
    "sagaId",
    "sagaOrder",
    "isLegendary",
    "posterFile",
    "tagline",
]


def norm_stem(value: str) -> str:
    return re.sub(r"[^a-z0-9]", "", Path(value).stem.lower())


def norm_title(value: str) -> str:
    return re.sub(r"[^a-z0-9]", "", (value or "").lower())


def poster_file_from_title(title: str) -> str:
    base = re.sub(r"[^A-Za-z0-9]", "", title)
    return f"{base}.png"


def split_lower_words(lower: str) -> str:
    words = []
    i = 0
    while i < len(lower):
        best = None
        for w in WORDS:
            if lower.startswith(w, i) and (best is None or len(w) > len(best)):
                best = w
        if best:
            words.append(best)
            i += len(best)
        else:
            j = i + 1
            while j < len(lower) and not any(lower.startswith(w, j) for w in WORDS):
                j += 1
            words.append(lower[i:j])
            i = j
    return " ".join(w.title() for w in words if w)


def title_from_stem(stem: str) -> str:
    overrides = {
        "wildhorizons": "Wild Horizons",
        "athousandfireflies": "A Thousand Fireflies",
        "beneaththeice": "Beneath the Ice",
        "hiddenrivers": "Hidden Rivers",
        "lifeabovetheclouds": "Life Above the Clouds",
        "secretsofthereef": "Secrets of the Reef",
        "desertkingdoms": "Desert Kingdoms",
        "thelostballoon": "The Lost Balloon",
        "theflyingorchard": "The Flying Orchard",
        "detectivepotato": "Detective Potato",
        "littlegiants": "Little Giants",
        "echoesoftitan": "Echoes of Titan",
        "songoftheforgottenrealm": "Song of the Forgotten Realm",
        "voiceofthedeep": "Voices of the Deep",
        "welcomehomesteve": "Welcome Home Steve",
    }
    key = norm_stem(stem)
    if key in overrides:
        return overrides[key]
    if re.search(r"[A-Z]", stem):
        s = re.sub(r"([a-z])([A-Z])", r"\1 \2", stem)
        s = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1 \2", s)
        return " ".join(p.title() for p in s.split())
    return split_lower_words(stem.lower())


def load_csv(path: Path):
    return list(csv.DictReader(path.open(encoding="utf-8-sig")))


def scan_posters():
    posters = []
    seen = set()
    for dirpath, _, files in os.walk(POSTERS_ROOT):
        folder = Path(dirpath).name
        if folder not in FOLDER_GENRE:
            continue
        genre = FOLDER_GENRE[folder]
        for fn in files:
            if not fn.lower().endswith((".png", ".jpg", ".jpeg", ".webp")):
                continue
            key = (genre, norm_stem(fn))
            if key in seen:
                continue
            seen.add(key)
            stem = Path(fn).stem
            posters.append(
                {
                    "genre": genre,
                    "diskFile": fn,
                    "stem": stem,
                    "norm": norm_stem(fn),
                }
            )
    return posters


def build_name_lookup():
    rows = load_csv(PREMIUM_NAMES)
    by_genre = defaultdict(dict)
    for row in rows:
        by_genre[row["genre"]][norm_title(row["movieName"])] = row["movieName"]
    return by_genre


def resolve_movie_name(poster, name_lookup, legendaries):
    genre = poster["genre"]
    leg_id, leg_name = legendaries.get(genre, (None, None))
    if leg_name:
        leg_norm = norm_title(leg_name)
        if poster["norm"] == leg_norm or leg_norm in poster["norm"] or poster["norm"] in leg_norm:
            return leg_name
        if "voice" in poster["norm"] and "deep" in poster["norm"]:
            return leg_name
    return title_from_stem(poster["stem"])


def rarity_rank(rarity: str) -> int:
    return {"Legendary": 3, "Epic": 2, "Rare": 1, "Common": 0}.get(rarity, 0)


def rebalance_genre_capacity(catalog, poster_by_genre):
    """When poster premium count exceeds genre catalog slots, reassign Common entries from donor genres."""
    by_genre = defaultdict(list)
    for r in catalog:
        by_genre[r["genre"]].append(r)

    for genre, posters in poster_by_genre.items():
        deficit = len(posters) - len(by_genre[genre])
        if deficit <= 0:
            continue

        donors = []
        for donor_genre, rows in by_genre.items():
            if donor_genre == genre:
                continue
            poster_count = len(poster_by_genre.get(donor_genre, []))
            spare = len(rows) - poster_count
            if spare > 0:
                donors.append((spare, donor_genre))

        donors.sort(reverse=True)
        for _, donor_genre in donors:
            if deficit <= 0:
                break
            candidates = [
                r
                for r in by_genre[donor_genre]
                if r["rarity"] == "Common" and not r.get("sagaId")
            ]
            candidates.sort(key=lambda r: r["catalogId"])
            while deficit > 0 and candidates:
                donor_row = candidates.pop()
                donor_row["genre"] = genre
                by_genre[genre].append(donor_row)
                by_genre[donor_genre].remove(donor_row)
                deficit -= 1

        if deficit > 0:
            raise SystemExit(
                f"{genre}: need {len(posters)} slots, have {len(by_genre[genre])} after rebalance"
            )


def assign_posters(catalog, posters, name_lookup):
    by_id = {r["catalogId"]: r for r in catalog}

    poster_by_genre = defaultdict(list)
    for p in posters:
        poster_by_genre[p["genre"]].append(p)

    rebalance_genre_capacity(catalog, poster_by_genre)

    by_genre = defaultdict(list)
    for r in catalog:
        by_genre[r["genre"]].append(r)

    assigned_poster_keys = set()
    premium_ids = set()

    # 1) Legendary slots
    for genre, (leg_id, leg_name) in LEGENDARIES.items():
        leg_norm = norm_title(leg_name)
        poster = None
        for p in poster_by_genre[genre]:
            if p["norm"] == leg_norm or leg_norm.startswith(p["norm"]) or p["norm"].startswith(leg_norm):
                poster = p
                break
        if poster is None and poster_by_genre[genre]:
            # fallback: best fuzzy for documentary voice/voices
            for p in poster_by_genre[genre]:
                if "voice" in p["norm"] and "deep" in p["norm"]:
                    poster = p
                    break
        if poster is None:
            continue
        row = by_id[leg_id]
        row["movieName"] = leg_name
        row["rarity"] = "Legendary"
        row["isLegendary"] = "1"
        if norm_stem(row.get("posterFile", "")) != poster["norm"]:
            row["posterFile"] = poster["file"]
        premium_ids.add(leg_id)
        assigned_poster_keys.add((genre, poster["norm"]))

    # 2) posterFile stem exact match
    for genre, rows in by_genre.items():
        for row in rows:
            if row["catalogId"] in premium_ids:
                continue
            stem = norm_stem(row.get("posterFile", ""))
            if not stem:
                continue
            for p in poster_by_genre[genre]:
                key = (genre, p["norm"])
                if key in assigned_poster_keys:
                    continue
                if p["norm"] == stem:
                    apply_premium_row(row, p, name_lookup)
                    premium_ids.add(row["catalogId"])
                    assigned_poster_keys.add(key)
                    break

    # 3) Fill remaining posters into catalog slots per genre
    for genre in by_genre:
        remaining_posters = [
            p
            for p in poster_by_genre[genre]
            if (genre, p["norm"]) not in assigned_poster_keys
        ]
        remaining_slots = [
            by_id[r["catalogId"]]
            for r in by_genre[genre]
            if r["catalogId"] not in premium_ids
        ]
        remaining_slots.sort(
            key=lambda r: (
                -rarity_rank(r.get("_orig_rarity", r["rarity"])),
                -int(r["cityUnlock"] or 0),
                r["catalogId"],
            )
        )
        remaining_slots = remaining_slots[: len(remaining_posters)]
        if len(remaining_posters) != len(remaining_slots):
            raise SystemExit(
                f"{genre}: posters={len(remaining_posters)} slots={len(remaining_slots)}"
            )
        for p, row in zip(remaining_posters, remaining_slots):
            apply_premium_row(row, p, name_lookup)
            premium_ids.add(row["catalogId"])
            assigned_poster_keys.add((genre, p["norm"]))

    if len(premium_ids) != 130:
        raise SystemExit(f"Expected 130 premium assignments, got {len(premium_ids)}")

    # 4) Demote non-premium to Common
    for row in by_id.values():
        if row["catalogId"] not in premium_ids:
            row["rarity"] = "Common"
            row["isLegendary"] = "0"

    # 5) Assign Epic/Rare among non-legendary premium (preserve prior Epic when possible)
    epic_budget = 20
    for genre, rows in by_genre.items():
        premium_rows = [by_id[r["catalogId"]] for r in rows if r["catalogId"] in premium_ids]
        for row in premium_rows:
            if row["rarity"] == "Legendary":
                continue
            if epic_budget > 0 and rarity_rank(row.get("_orig_rarity", row["rarity"])) >= 2:
                row["rarity"] = "Epic"
                epic_budget -= 1
            else:
                row["rarity"] = "Rare"
                row["isLegendary"] = "0"

    return catalog


def apply_premium_row(row, poster, name_lookup):
    name = resolve_movie_name(poster, name_lookup, LEGENDARIES)
    row["movieName"] = name
    existing_norm = norm_stem(row.get("posterFile", ""))
    if existing_norm != poster["norm"]:
        row["posterFile"] = poster_file_from_title(name)
    row["isLegendary"] = "0"


def write_csv(path, rows):
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=HEADER, extrasaction="ignore", quoting=csv.QUOTE_MINIMAL)
        w.writeheader()
        for row in sorted(rows, key=lambda r: (r["genre"], int(r["cityUnlock"]), r["catalogId"])):
            w.writerow({k: row.get(k, "") for k in HEADER})


def build_summary(rows):
    lines = []
    lines.append("=" * 72)
    lines.append("CATÁLOGO 5.0 — Resumen integración premium definitiva")
    lines.append("=" * 72)
    lines.append("")
    lines.append(f"Total películas : {len(rows)}")
    lines.append("")
    lines.append("Por rareza:")
    for rarity in ("Common", "Rare", "Epic", "Legendary"):
        c = sum(1 for r in rows if r["rarity"] == rarity)
        lines.append(f"  {rarity:<12}: {c}")
    lines.append("")
    lines.append("Por género (total / Common / Premium):")
    genre_order = [
        "Action",
        "SciFi",
        "Drama",
        "Horror",
        "Comedy",
        "Fantasy",
        "Romance",
        "Thriller",
        "Animation",
        "Documentary",
    ]
    for g in genre_order:
        g_rows = [r for r in rows if r["genre"] == g]
        common = sum(1 for r in g_rows if r["rarity"] == "Common")
        premium = len(g_rows) - common
        lines.append(f"  {g:<14} total={len(g_rows):>2}  Common={common:>2}  Premium={premium:>2}")
    lines.append("")
    lines.append("Archivos generados:")
    lines.append(f"  Master : {MASTER_OUT}")
    lines.append(f"  Premium: {PREMIUM_OUT}")
    lines.append(f"  Common : {COMMON_OUT}")
    return "\n".join(lines)


def main():
    catalog = load_csv(MASTER_IN)
    for r in catalog:
        r["_orig_rarity"] = r["rarity"]

    posters = scan_posters()
    if len(posters) != 130:
        raise SystemExit(f"Expected 130 poster files, found {len(posters)}")

    name_lookup = build_name_lookup()
    integrated = assign_posters(catalog, posters, name_lookup)

    premium = [r for r in integrated if r["rarity"] != "Common"]
    common = [r for r in integrated if r["rarity"] == "Common"]

    if len(premium) != 130 or len(common) != 170:
        raise SystemExit(f"Split mismatch premium={len(premium)} common={len(common)}")

    write_csv(MASTER_OUT, integrated)
    write_csv(PREMIUM_OUT, premium)
    write_csv(COMMON_OUT, common)

    summary = build_summary(integrated)
    SUMMARY_OUT.parent.mkdir(parents=True, exist_ok=True)
    SUMMARY_OUT.write_text(summary, encoding="utf-8")

    print(summary)


if __name__ == "__main__":
    main()
