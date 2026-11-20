---
model: opus
max_turns: 65
---

# T11 — Extract the inspection core into a testable library

## Goal
Move the 21 inspection algorithms and pure helpers out of the WinForms form into a UI-free class library. Behaviour must stay bit-for-bit identical, so it can be unit tested (T12) and used by the simulation (T13).

## Context
- Algorithms: `RealVisionLab/MainPage.cs:1239-2304` (functions 0-20), helpers `ConvertToByte` (`:2308`), `TransformPoint` (`:2340`), `Threshold_Apply` (`:2353`).
- The dispatch is duplicated twice (`:934-996` and `:1002-1058`). The direction logic uses `yön`, `R-`, `L-`, `islem` tokens in the part name (`:913-937`).
- Algorithms read/write shared statics: `Ofset`, `hata`, `Thrs`, `Rect`, `bmp`, `rectString`. Functions 10, 18, 19 and 20 also touch UI or I/O (`RotUzunluk`, `txt_qr`, PLC). Those parts stay in the form behind callbacks.
- After T09, config models are in `RealVisionLab/Config/`.

## Scope
- New project `RealVisionLab.Core/RealVisionLab.Core.csproj`: classic or SDK-style, targeting `net472`, referencing only `System.Drawing` and `zxing` (for QR). Add it to `RealVisionLab.sln`. `RealVisionLab.csproj` gets a ProjectReference.
- Move: `Config/*` (from T09), the pure algorithms (as `IInspection` implementations or static methods returning `InspectionResult { int Value; bool Ok; Bitmap Overlay; int? Min/Max override }`), `ConvertToByte`, `TransformPoint`, threshold.
- Do NOT fix measurement bugs (functions 6/7 variable mix-ups, `R_Iki…` overlay column, L/R comment mismatch). Preserve them exactly and add `// KNOWN-ISSUE (AUDIT §2)` comments. Fixing them is an owner decision.
- Do NOT change UI layout.

## Steps
1. Create the Core project and an `InspectionContext` (ROI index, camera index, threshold, min/max, part name, access to other ROI results for function 17, `Rect[]` for function 9).
2. Port each function 1:1. Replace writes to `Ofset[r_n,1]`/`hata` with return values. Keep functions 9 and 11 semantics: they mutate other ROIs' rectangles / Ofset. Model this explicitly in the result (`RectAdjustments`, `OfsetOverride`).
3. Replace the duplicated dispatch in `Islemler` with a single table lookup `InspectionRegistry.Get(islemId)` plus the direction filter.
4. Keep UI side effects (RotUzunluk text, txt_qr, Btn_bekle, Qr_Oku_harici) in MainPage as callbacks/adapters.
5. Build both projects.

## Acceptance criteria
- [ ] `RealVisionLab.Core` has no reference to `System.Windows.Forms`, `Sharp7`, `System.IO.Ports` or AForge.
- [ ] `MainPage.cs` no longer contains the 21-branch `if/else` dispatch (only one registry call).
- [ ] Every algorithm in Core has a `// ported from MainPage.cs:<line>` reference comment for reviewer diffing.
- [ ] The solution builds in Debug and Release.

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release
git grep -n "System.Windows.Forms\|Sharp7\|System.IO.Ports" -- RealVisionLab.Core
```
