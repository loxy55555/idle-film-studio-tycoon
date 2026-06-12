#!/usr/bin/env python3
"""FASE 5.4 — Catalog finalization report (post-fix verification)."""
import csv
import os
import re
import sys
from collections import Counter
from difflib import SequenceMatcher
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "Assets" / "Data" / "Imports"))

from catalog_audit_5_1_5_3 import (  # noqa: E402
    LEGENDARIES,
    audit_legendary_conflicts,
    audit_posters,
    load_csv,
    norm,
    norm_stem,
    scan_posters,
)

MASTER = ROOT / "Assets" / "Data" / "Imports" / "MovieCatalog_Integrated_300.csv"
PREMIUM = ROOT / "Assets" / "Data" / "Exports" / "PremiumCatalogFinal.csv"
COMMON = ROOT / "Assets" / "Data" / "Exports" / "CommonCatalogFinal.csv"
REPORT = ROOT / "Assets" / "Data" / "Reports" / "CatalogFinalizationReport.txt"

RENAMES = {
    "Animation_Little_Giants_II": ("Little Giants II", "Big Trouble Club"),
    "Romance_A_Sky_for_Two_II": ("A Sky for Two II", "Horizons Apart"),
}
POSTER_FIX = ("Documentary_Legendary", "VoicesoftheDeep.png", "voiceofthedeep.png")


def premium_stats(poster_report):
    premium_rows = [r for r in poster_report if r["RecordType"] == "Premium"]
    orphans = [r for r in poster_report if r["RecordType"] == "Orphan"]
    dups = [r for r in poster_report if r["RecordType"] == "Duplicate"]
    ok = [r for r in premium_rows if r["status"] == "OK"]
    missing = [r for r in premium_rows if r["status"] != "OK"]
    return {
        "premium_total": len(premium_rows),
        "posters_on_disk": len(scan_posters()),
        "assigned_ok": len(ok),
        "missing": missing,
        "orphans": orphans,
        "duplicates": dups,
    }


def verify_renames(master_rows):
    by_id = {r["catalogId"]: r for r in master_rows}
    issues = []
    for cid, (old, new) in RENAMES.items():
        row = by_id.get(cid)
        if not row:
            issues.append(f"Missing catalogId: {cid}")
            continue
        if row["movieName"] == old:
            issues.append(f"{cid} still named '{old}'")
        elif row["movieName"] != new:
            issues.append(f"{cid} unexpected name '{row['movieName']}' (expected '{new}')")
    return issues


def verify_poster_fix(master_rows):
    by_id = {r["catalogId"]: r for r in master_rows}
    cid, old_pf, new_pf = POSTER_FIX
    row = by_id.get(cid)
    if not row:
        return [f"Missing {cid}"]
    pf = row.get("posterFile", "")
    if norm_stem(pf) != norm_stem(new_pf):
        return [f"{cid} posterFile={pf!r} (expected {new_pf!r})"]
    return []


def write_report(lines):
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text("\n".join(lines) + "\n", encoding="utf-8")


def main():
    master = load_csv(MASTER)
    common = load_csv(COMMON)
    premium = load_csv(PREMIUM)

    rename_issues = verify_renames(master)
    poster_issues = verify_poster_fix(master)

    legendary_report = audit_legendary_conflicts(common)
    poster_index = scan_posters()
    poster_report = audit_posters(premium, poster_index)
    stats = premium_stats(poster_report)

    critical = [r for r in legendary_report if r["Severity"] == "Critical"]
    minor = [r for r in legendary_report if r["Severity"] == "Minor"]

    lines = [
        "FASE 5.4 — Catalog Finalization Report",
        "=" * 44,
        "",
        "CHANGES APPLIED",
        f"  Animation_Little_Giants_II : Little Giants II -> Big Trouble Club",
        f"  Romance_A_Sky_for_Two_II : A Sky for Two II -> Horizons Apart",
        f"  Documentary_Legendary    : VoicesoftheDeep.png -> voiceofthedeep.png",
        "",
        "RENAME VERIFICATION",
    ]
    if rename_issues:
        lines.extend(f"  FAIL: {i}" for i in rename_issues)
    else:
        lines.append("  OK — legendary conflict renames applied")

    lines.append("")
    lines.append("POSTER FILE VERIFICATION")
    if poster_issues:
        lines.extend(f"  FAIL: {i}" for i in poster_issues)
    else:
        lines.append("  OK — Voices of the Deep posterFile matches disk")

    lines.extend([
        "",
        "POSTER ASSIGNMENT (Premium)",
        f"  Premium total        : {stats['premium_total']}",
        f"  Posters on disk      : {stats['posters_on_disk']}",
        f"  Posters assigned OK  : {stats['assigned_ok']}",
        f"  Posters missing      : {len(stats['missing'])}",
        f"  Posters orphan       : {len(stats['orphans'])}",
        f"  Duplicate refs       : {len(stats['duplicates'])}",
        "",
        "TARGET: 130 Premium | 130 Posters | 130 Assignments | 0 Missing | 0 Orphan | 0 Duplicate",
    ])

    target_ok = (
        stats["premium_total"] == 130
        and stats["posters_on_disk"] == 130
        and stats["assigned_ok"] == 130
        and len(stats["missing"]) == 0
        and len(stats["orphans"]) == 0
        and len(stats["duplicates"]) == 0
    )
    lines.append(f"  STATUS               : {'PASS' if target_ok else 'FAIL'}")
    lines.append("")

    if stats["missing"]:
        lines.append("POSTERS FALTANTES")
        for r in stats["missing"]:
            lines.append(f"  {r['catalogId']}  posterFile={r['posterFile']}  ({r['status']})")
        lines.append("")

    if stats["orphans"]:
        lines.append("POSTERS HUÉRFANOS")
        for r in stats["orphans"]:
            lines.append(f"  {r['posterFile']}  ({r['detail']})")
        lines.append("")

    if stats["assigned_ok"] == stats["premium_total"] and not stats["missing"]:
        lines.append("POSTERS ASIGNADOS")
        lines.append(f"  All {stats['assigned_ok']} premium entries matched on disk.")
        lines.append("")

    lines.extend([
        "LEGENDARY CONFLICTS (remaining)",
        f"  Total    : {len(legendary_report)}",
        f"  Critical : {len(critical)}",
        f"  Minor    : {len(minor)}",
    ])
    if legendary_report:
        for r in legendary_report:
            lines.append(
                f"  [{r['Severity']}] {r['CommonMovie']} vs {r['LegendaryConflict']} ({r['ConflictType']})"
            )
    else:
        lines.append("  (none)")

    lines.extend([
        "",
        "CATALOG STATUS",
        f"  Master CSV rows : {len(master)}",
        f"  Common rows     : {len(common)}",
        f"  Premium rows    : {len(premium)}",
        f"  Production ready: {'YES' if target_ok and not rename_issues and not poster_issues and not critical else 'NO'}",
        "",
        f"Report: {REPORT.relative_to(ROOT).as_posix()}",
    ])

    write_report(lines)

    print(f"Wrote {REPORT.relative_to(ROOT)}")
    print(f"Poster target: {'PASS' if target_ok else 'FAIL'}")
    print(f"Legendary critical remaining: {len(critical)}")
    return 0 if target_ok and not critical and not rename_issues and not poster_issues else 1


if __name__ == "__main__":
    raise SystemExit(main())
