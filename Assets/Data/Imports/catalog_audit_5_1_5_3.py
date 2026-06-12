#!/usr/bin/env python3
"""FASE 5.1–5.3 — Catalog audits (read-only)."""
import csv
import os
import re
from collections import Counter, defaultdict
from difflib import SequenceMatcher
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
MASTER = ROOT / "Assets" / "Data" / "Imports" / "MovieCatalog_Integrated_300.csv"
PREMIUM = ROOT / "Assets" / "Data" / "Exports" / "PremiumCatalogFinal.csv"
COMMON = ROOT / "Assets" / "Data" / "Exports" / "CommonCatalogFinal.csv"
POSTERS = ROOT / "Assets" / "Posters"
REPORTS = ROOT / "Assets" / "Data" / "Reports"

LEGENDARIES = {
    "Action": "Iron Wolves",
    "SciFi": "Echoes of Titan",
    "Drama": "When the River Sleeps",
    "Horror": "The Hollow House",
    "Comedy": "Detective Potato",
    "Fantasy": "Song of the Forgotten Realm",
    "Romance": "A Sky for Two",
    "Thriller": "Red File 27",
    "Animation": "Little Giants",
    "Documentary": "Voices of the Deep",
}

SAGA_EXPECTED = {
    "project_avalanche": 6,
    "dominion": 6,
    "atlas_signal": 4,
    "the_long_road": 4,
    "hollow_creek": 4,
    "moonkeeper": 4,
    "the_last_colony": 6,
    "winter_letters": 2,
    "blackwater": 4,
    "uncle_gary": 2,
    "the_hidden_crown": 4,
    "the_glass_forest": 2,
    "beneath_the_summer_sky": 2,
    "the_black_ledger": 4,
    "shadow_trigger": 4,
}

STOP = {
    "the", "a", "an", "of", "for", "in", "on", "at", "to", "and", "with", "from",
    "by", "or", "as", "is", "it", "its", "two", "one", "last", "new",
}

THEMATIC = {
    "Iron Wolves": ["wolf", "wolves", "iron"],
    "Echoes of Titan": ["titan", "echo", "echoes"],
    "When the River Sleeps": ["river", "sleep", "sleeps"],
    "The Hollow House": ["hollow", "house"],
    "Detective Potato": ["detective", "potato"],
    "Song of the Forgotten Realm": ["forgotten", "realm", "song"],
    "A Sky for Two": ["sky", "two"],
    "Red File 27": ["red", "file"],
    "Little Giants": ["little", "giants", "giant"],
    "Voices of the Deep": ["voice", "voices", "deep"],
}

FOLDER_GENRE = {
    "accion": "Action", "action": "Action", "sci-fi": "SciFi", "scifi": "SciFi",
    "drama": "Drama", "terror": "Horror", "horror": "Horror", "comedia": "Comedy",
    "comedy": "Comedy", "fantasia": "Fantasy", "fantasy": "Fantasy", "romance": "Romance",
    "thriller": "Thriller", "animacion": "Animation", "animación": "Animation",
    "animation": "Animation", "documental": "Documentary", "documentary": "Documentary",
}

SEQUEL_RE = re.compile(
    r"\b(ii|iii|iv|v|vi|vii|viii|ix|x|\d+|part\s*\d+|returns|reborn|legacy|continues|awakens)\b",
    re.I,
)
NUMBERED_RE = re.compile(r"\b(ii|iii|iv|v|vi|vii|viii|ix|x|\d+)\b", re.I)


def load_csv(path):
    return list(csv.DictReader(path.open(encoding="utf-8-sig")))


def norm(s):
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


def norm_stem(path_or_name):
    return norm(Path(path_or_name).stem)


def tokens(title):
    return {t for t in re.findall(r"[a-z]+", (title or "").lower()) if t not in STOP and len(t) > 2}


def strip_sequel(title):
    return re.sub(
        r"\s+(ii|iii|iv|v|vi|vii|viii|ix|x|\d+|part\s+\d+).*$",
        "",
        title or "",
        flags=re.I,
    ).strip()


def similarity(a, b):
    return SequenceMatcher(None, norm(a), norm(b)).ratio()


def scan_posters():
    found = {}
    for dirpath, _, files in os.walk(POSTERS):
        folder = Path(dirpath).name
        genre = FOLDER_GENRE.get(folder)
        if not genre:
            continue
        for fn in files:
            if not fn.lower().endswith((".png", ".jpg", ".jpeg", ".webp")):
                continue
            key = norm_stem(fn)
            found[key] = {"file": fn, "path": str(Path(dirpath) / fn), "genre": genre}
    return found


def audit_legendary_conflicts(common_rows):
    rows = []
    for movie in common_rows:
        genre = movie["genre"]
        legendary = LEGENDARIES.get(genre)
        if not legendary:
            continue
        name = movie["movieName"]
        conflict_type = None
        severity = None
        detail = ""

        base_leg = strip_sequel(legendary)
        base_name = strip_sequel(name)
        leg_norm = norm(base_leg)
        name_norm = norm(base_name)

        if norm(name) == norm(legendary):
            conflict_type, severity = "Duplicate Name", "Critical"
        elif leg_norm and leg_norm in norm(name):
            conflict_type = "Sequel" if SEQUEL_RE.search(name) else "Spin-off"
            severity = "Critical"
        elif name_norm and name_norm in norm(legendary) and name_norm != leg_norm:
            conflict_type = "Prequel"
            severity = "Critical"
        elif genre == "Thriller" and legendary == "Red File 27":
            if re.search(r"red\s*file\s*\d+", name, re.I) and norm(name) != norm(legendary):
                conflict_type, severity = "Related Numbering", "Critical"
        elif similarity(base_name, base_leg) >= 0.82:
            conflict_type, severity = "Similar Title", "Critical"
        elif similarity(base_name, base_leg) >= 0.68:
            conflict_type, severity = "Similar Title", "Minor"
        else:
            shared = tokens(base_name) & tokens(base_leg)
            if len(shared) >= 2:
                conflict_type, severity = "Spin-off", "Minor"
                detail = f"Shared tokens: {', '.join(sorted(shared))}"
            else:
                theme = THEMATIC.get(legendary, [])
                theme_hits = [w for w in theme if w in (name or "").lower()]
                if len(theme_hits) >= 2 or (
                    len(theme_hits) == 1 and theme_hits[0] in {"wolves", "titan", "potato", "giants"}
                ):
                    conflict_type, severity = "Thematic Conflict", "Minor"
                    detail = f"Thematic overlap: {', '.join(theme_hits)}"
                elif SEQUEL_RE.search(name) and shared:
                    conflict_type, severity = "Sequel", "Minor"
                    detail = f"Numbered title shares: {', '.join(sorted(shared))}"

        if conflict_type:
            rows.append(
                {
                    "CommonMovie": name,
                    "Genre": genre,
                    "LegendaryConflict": legendary,
                    "ConflictType": conflict_type,
                    "Severity": severity,
                    "Detail": detail,
                }
            )

    rows.sort(key=lambda r: (r["Severity"] != "Critical", r["Genre"], r["CommonMovie"]))
    return rows


def audit_posters(premium_rows, poster_index):
    report = []
    used_keys = Counter()
    assigned_keys = set()

    for m in premium_rows:
        pf = (m.get("posterFile") or "").strip()
        key = norm_stem(pf) if pf else ""
        used_keys[key] += 1 if key else 0
        found = poster_index.get(key) if key else None
        status = "OK"
        issue = ""

        if not pf:
            status = "Missing posterFile"
        elif not found:
            status = "Poster Not Found"
        else:
            assigned_keys.add(key)
            if found["genre"] != m["genre"]:
                status = "Genre Folder Mismatch"
                issue = f"Poster in {found['genre']}, movie in {m['genre']}"
            name_sim = similarity(m["movieName"], Path(found["file"]).stem)
            if name_sim < 0.45 and norm(m["movieName"]) != key:
                status = "Name Mismatch"
                issue = f"movieName vs file stem similarity={name_sim:.2f}"

        if used_keys[key] > 1 and key:
            status = "Duplicate posterFile"
            issue = f"Used by {used_keys[key]} premium entries"

        report.append(
            {
                "RecordType": "Premium",
                "catalogId": m["catalogId"],
                "movieName": m["movieName"],
                "genre": m["genre"],
                "posterFile": pf,
                "status": status,
                "detail": issue,
            }
        )

    premium_keys = {norm_stem(m.get("posterFile", "")) for m in premium_rows if m.get("posterFile")}
    for key, info in poster_index.items():
        if key not in premium_keys:
            report.append(
                {
                    "RecordType": "Orphan",
                    "catalogId": "",
                    "movieName": "",
                    "genre": info["genre"],
                    "posterFile": info["file"],
                    "status": "Orphan Poster",
                    "detail": info["path"],
                }
            )

    dup_files = [k for k, c in used_keys.items() if k and c > 1]
    for key in dup_files:
        report.append(
            {
                "RecordType": "Duplicate",
                "catalogId": "",
                "movieName": "",
                "genre": "",
                "posterFile": key,
                "status": "Duplicate posterFile",
                "detail": f"{used_keys[key]} references",
            }
        )

    return report


def audit_sagas(all_rows):
    report = []
    by_saga = defaultdict(list)
    numbered_standalone = []

    for m in all_rows:
        saga = (m.get("sagaId") or "").strip()
        order = int(m.get("sagaOrder") or 0)
        is_leg = m.get("isLegendary") in ("1", "true", "True")

        if saga:
            by_saga[saga].append(m)
            if is_leg:
                report.append(
                    {
                        "catalogId": m["catalogId"],
                        "movieName": m["movieName"],
                        "genre": m["genre"],
                        "sagaId": saga,
                        "sagaOrder": order,
                        "issueType": "Legendary In Saga",
                        "severity": "Critical",
                        "detail": "Legendaries must be standalone",
                    }
                )
            if order <= 0:
                report.append(
                    {
                        "catalogId": m["catalogId"],
                        "movieName": m["movieName"],
                        "genre": m["genre"],
                        "sagaId": saga,
                        "sagaOrder": order,
                        "issueType": "Invalid sagaOrder",
                        "severity": "Critical",
                        "detail": "sagaOrder must be >= 1",
                    }
                )
            if saga not in SAGA_EXPECTED:
                report.append(
                    {
                        "catalogId": m["catalogId"],
                        "movieName": m["movieName"],
                        "genre": m["genre"],
                        "sagaId": saga,
                        "sagaOrder": order,
                        "issueType": "Unknown sagaId",
                        "severity": "Minor",
                        "detail": "Not in approved saga registry",
                    }
                )
        elif NUMBERED_RE.search(m["movieName"] or ""):
            numbered_standalone.append(m)

    for saga_id, expected in SAGA_EXPECTED.items():
        entries = by_saga.get(saga_id, [])
        if len(entries) != expected:
            report.append(
                {
                    "catalogId": "",
                    "movieName": "",
                    "genre": "",
                    "sagaId": saga_id,
                    "sagaOrder": "",
                    "issueType": "Incomplete Saga",
                    "severity": "Critical",
                    "detail": f"Has {len(entries)} entries, expected {expected}",
                }
            )
        orders = [int(e.get("sagaOrder") or 0) for e in entries]
        if orders and sorted(orders) != list(range(1, len(orders) + 1)):
            report.append(
                {
                    "catalogId": "",
                    "movieName": "",
                    "genre": "",
                    "sagaId": saga_id,
                    "sagaOrder": "",
                    "issueType": "Saga Order Gap",
                    "severity": "Critical",
                    "detail": f"Orders: {sorted(orders)}",
                }
            )
        if len(orders) != len(set(orders)):
            report.append(
                {
                    "catalogId": "",
                    "movieName": "",
                    "genre": "",
                    "sagaId": saga_id,
                    "sagaOrder": "",
                    "issueType": "Duplicate sagaOrder",
                    "severity": "Critical",
                    "detail": f"Orders: {orders}",
                }
            )

    for m in numbered_standalone:
        report.append(
            {
                "catalogId": m["catalogId"],
                "movieName": m["movieName"],
                "genre": m["genre"],
                "sagaId": "",
                "sagaOrder": 0,
                "issueType": "Numbered Without Saga",
                "severity": "Minor",
                "detail": "Title suggests sequel numbering but sagaId is empty",
            }
        )

    return report, by_saga


def write_csv(path, fieldnames, rows):
    REPORTS.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8", newline="") as f:
        w = csv.DictWriter(f, fieldnames=fieldnames, extrasaction="ignore")
        w.writeheader()
        w.writerows(rows)


def main():
    master = load_csv(MASTER)
    common = load_csv(COMMON)
    premium = load_csv(PREMIUM)
    poster_index = scan_posters()

    # 5.1
    leg_rows = audit_legendary_conflicts(common)
    write_csv(
        REPORTS / "LegendaryConflictReport.csv",
        ["CommonMovie", "Genre", "LegendaryConflict", "ConflictType", "Severity", "Detail"],
        leg_rows,
    )

    # 5.2
    poster_rows = audit_posters(premium, poster_index)
    write_csv(
        REPORTS / "PosterAssignmentReport.csv",
        ["RecordType", "catalogId", "movieName", "genre", "posterFile", "status", "detail"],
        poster_rows,
    )

    # 5.3
    saga_rows, by_saga = audit_sagas(master)
    write_csv(
        REPORTS / "SagaAuditReport.csv",
        ["catalogId", "movieName", "genre", "sagaId", "sagaOrder", "issueType", "severity", "detail"],
        saga_rows,
    )

    leg_critical = sum(1 for r in leg_rows if r["Severity"] == "Critical")
    leg_minor = sum(1 for r in leg_rows if r["Severity"] == "Minor")

    premium_ok = sum(1 for r in poster_rows if r["RecordType"] == "Premium" and r["status"] == "OK")
    premium_missing = sum(
        1
        for r in poster_rows
        if r["RecordType"] == "Premium" and r["status"] in ("Missing posterFile", "Poster Not Found")
    )
    orphans = sum(1 for r in poster_rows if r["RecordType"] == "Orphan")
    name_mismatch = sum(1 for r in poster_rows if r["RecordType"] == "Premium" and r["status"] == "Name Mismatch")
    dup_posters = sum(1 for r in poster_rows if r["RecordType"] == "Duplicate")

    saga_movies = sum(len(v) for v in by_saga.values())
    standalone = len(master) - saga_movies
    saga_errors = len(saga_rows)
    saga_critical = sum(1 for r in saga_rows if r["severity"] == "Critical")

    summary = f"""
FASE 5.1 — Legendary Conflict Audit
  Common audited     : {len(common)}
  Total conflicts    : {len(leg_rows)}
  Critical           : {leg_critical}
  Minor              : {leg_minor}
  Report             : Assets/Data/Reports/LegendaryConflictReport.csv

FASE 5.2 — Poster Assignment Audit
  Premium total      : {len(premium)}
  Posters on disk    : {len(poster_index)}
  Posters assigned OK: {premium_ok}
  Posters missing    : {premium_missing}
  Name mismatches    : {name_mismatch}
  Orphan posters     : {orphans}
  Duplicate refs     : {dup_posters}
  Report             : Assets/Data/Reports/PosterAssignmentReport.csv

FASE 5.3 — Saga Audit
  Total sagas        : {len(SAGA_EXPECTED)}
  Movies in sagas    : {saga_movies}
  Standalone movies  : {standalone}
  Issues found       : {saga_errors}
  Critical issues    : {saga_critical}
  Report             : Assets/Data/Reports/SagaAuditReport.csv
"""
    print(summary.strip())
    (REPORTS / "CatalogAudit_5_1_5_3_Summary.txt").write_text(summary.strip(), encoding="utf-8")


if __name__ == "__main__":
    main()
