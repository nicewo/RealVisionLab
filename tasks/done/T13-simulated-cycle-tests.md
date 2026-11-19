---
model: sonnet
max_turns: 55
---

# T13 — Simulated inspection cycle and integration tests

## Goal
Verify the full cycle (start → outputs → capture → inspect → OK/NOK outputs → reset) without hardware, using `SimulatedMachineIo` and image-file frames.

## Context
- `IMachineIo` / `SimulatedMachineIo` (T04), safe-state rules (T05), fail-closed (T03), Core library (T11), tests project (T12).
- Cycle logic currently lives in `MainPage.Islemler` (`RealVisionLab/MainPage.cs:840`) and `Reset` (`:431`). Output sequence today:
  - start: Lamba=1, Piston=1, Kase=0, Bos=0
  - OK: all four = 1, then after `Bekleme[6]` Kase=0, Bos=0
  - reset: all four = 0
- The debug image injection already exists (`takePhoto`, `sec_Click` `:2663`).

## Scope
- Extract the cycle sequencing into `RealVisionLab.Core/Cycle/InspectionCycle.cs`. It takes `IMachineIo`, an `IFrameSource`, the ROI config, wait times and an `IClock`/delay abstraction so tests run fast. `MainPage` calls it; UI updates happen through events.
- Add `ImageFileFrameSource` (Core) and use it in Debug when `SimulateIo=true` and a folder `SimFrames/` exists next to the exe.
- Tests in `RealVisionLab.Tests`.
- Do NOT change output values or ordering. Any difference from the sequence above must be flagged in the summary, not silently changed.

## Steps
1. Define `IFrameSource { bool TryGetFrame(int cam, TimeSpan timeout, out Bitmap frame); }`.
2. Move the sequencing from `Islemler` into `InspectionCycle.RunAsync`. MainPage keeps the drawing and labels.
3. Tests (with a fake clock):
   - OK part → recorded outputs equal the sequence above.
   - NOK part with RST=false → no Kase output, cycle waits for reset.
   - NOK with RST=true → auto reset after `Bekleme[8]`.
   - No frame → NOK, no Kase, safe state.
   - Exception inside an inspection → NOK, `ForceSafeState` called.
   - I/O disconnect mid-cycle → abort + safe state.
   - Start refused when a camera is missing.
4. Document how to run the app in simulation mode in `docs/BUILD.md` (a section, no new doc).

## Acceptance criteria
- [ ] All scenario tests above exist and pass via `build.ps1 -Test`.
- [ ] `MainPage.Islemler` delegates to `InspectionCycle` (no duplicated sequencing).
- [ ] Results are labelled "simülasyonda doğrulandı, donanımda doğrulanmadı" in the summary.

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug -Test
```
