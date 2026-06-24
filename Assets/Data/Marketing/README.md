# Marketing Save (FASE 21)

Editor-only snapshot for Play Store screenshots and promo captures.

## Generate

**Tools → Idle Film → Generate Marketing Save**

Writes `marketing_save_v7.json` in this folder. Does not touch your active save.

## Activate (Play Mode)

1. **Tools → Idle Film → Marketing Save → Install Marketing Save**
2. Confirm the dialog (backs up current `save_v2.json` → `save_v2.user_backup.json`)
3. Enter or restart **Play Mode**

Active save path (Editor):
`%LOCALAPPDATA%/../LocalLow/<CompanyName>/<ProductName>/save_v2.json`

## Return to normal play

**Tools → Idle Film → Marketing Save → Restore User Save from Backup**

Then restart Play Mode.

## Production builds

- Script lives in `Assets/Scripts/Editor/` — **not included in APK/AAB**
- Marketing JSON is inert unless manually installed via Editor menu
- Normal player saves are never modified unless you confirm Install
