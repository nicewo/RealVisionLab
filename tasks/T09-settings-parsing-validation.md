---
model: sonnet
max_turns: 55
---

# T09 — Settings parsing, validation and Property screen bugs

## Goal
Parse the string-encoded settings once into typed models with validation. Fix the confirmed bugs that corrupt configuration. The stored format stays byte-compatible.

## Context
- Format (comment `Property.cs:71`): `Rect` = `/`-separated records of `X,Y,W,H,MinOfset,MaxOfset,CamId,Islem,Threshold,ResetOrStop,PartName`. `Cam` = `/`-separated `camIndex,resIndex`. `Bekleme` = 11 `-`-separated ms values. `PlcFonk` = `/`-separated `db,start,size,camId,roiNo`.
- Parsing is scattered: `MainPage.cs:238-325, 365-370, 621-624, 853-854, 911-937, 1105` and `Property.cs:107-111, 261-492, 694-971`.
- Bugs:
  - `btn_delCam_Click` splits/joins `Cam` with `','` instead of `'/'` (`Property.cs:1179, 1199`).
  - `btn_delCam_Click` reopens the form with the ROI field `index` (`:1207`) instead of a camera tab index.
  - `btn_bit_Click` resets `nud_Lamba` in every branch (`Property.cs:1654-1656`).
  - UART bit NUDs load as 1-based in the constructor (`:95-98`) but 0-based in `Property_Load` (`:1311-1314`), and are saved with `-1` (`:1657-1660`). `button2_Click` also saves them without `-1` (`:1410-1413`). Pick one convention: the UI shows 1-based, storage is 0-based.
  - The part name (field 10) can contain `,` or `/` and corrupts `Rect`.
  - `MainPage_Load` throws on an empty daily log file (`MainPage.cs:175-192`).

## Scope
- New `RealVisionLab/Config/` with `RoiConfig`, `CameraConfig`, `WaitTimes` (named properties for the 11 indices, with names inferred from usage in `Islemler`/`Reset`), `ConfigParser` (Parse/Serialize, `TryParse` with error list).
- Update call sites in `MainPage.cs`/`Property.cs` to use the parser. Fix the listed bugs.
- Do NOT change the persisted string format. Do NOT add new settings.

## Steps
1. Implement `ConfigParser` with round-trip `Serialize(Parse(s)) == s` for valid strings (including `True/False` casing as stored today).
2. Validation: integer fields parse; `W,H > 0`; `0 <= Islem <= 20`; `0 <= Threshold <= 255`; `CamId < camera count`; `Min <= Max`. Invalid records are reported (alarm text + `debug.txt`) and treated as NOK ROIs at runtime. They must not crash the app.
3. Sanitize the part name on input (`txb_parca`): strip `,` and `/`.
4. Fix the bugs listed above.
5. `MainPage_Load`: an empty or unparsable log gives `tarama_count = 0` without throwing.

## Acceptance criteria
- [ ] `ConfigParser` round-trips the default `Rect`, `Cam`, `Bekleme` and `PlcFonk` values from `Properties/Settings.settings` (asserted by T12 tests; until then, by a Debug-only self-check or a documented manual check).
- [ ] `Property.cs` contains no `Settings.Default.Cam.Split(',')`.
- [ ] `btn_bit_Click` corrects each NUD independently.
- [ ] UART bit numbering is consistent (1-based UI, 0-based storage) in all four places.
- [ ] Debug build passes.

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "Cam.Split(',')" -- RealVisionLab
```
