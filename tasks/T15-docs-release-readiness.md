---
model: haiku
max_turns: 40
---

# T15 — Documentation and release readiness

## Goal
Make the docs match the code: correct the README claims, and document config formats, I/O mapping, commissioning, troubleshooting and the release process.

## Context
- `README.md`, `README.tr.md`, `docs/BUILD.md` (T02/T13), `docs/AUDIT.md` (§2 feature table, §5).
- README claims that are wrong today (AUDIT §2):
  - "AForge.Imaging filter pipeline": not used; processing is custom pixel code.
  - "ZBar": unused.
  - "behind a password screen": only after T08.
  - "writing result bits into DB areas": DB1.DBB0 only; the PlcFonk screen is unused.
- Config formats: `RealVisionLab/Config/*` (T09). Output bit mapping: `RealVisionLab/Io/*` (T04).

## Scope
- Edit `README.md`, `README.tr.md` (keep them in sync). Create `docs/CONFIG.md`, `docs/COMMISSIONING.md`, `docs/RELEASE.md`, `docs/KNOWN_ISSUES.md`.
- Docs only. No code changes. Never include passwords, hashes, IPs of real machines or other secrets.

## Steps
1. README (EN + TR): fix the four claims above based on the code as it is at this point (re-check each against source before writing). Add "Status / limitations" and links to the new docs.
2. `CONFIG.md`: field-by-field tables for `Rect`, `Cam`, `Bekleme` (11 named indices), `PlcFonk` (mark it as unused at runtime if still true), UART/PLC bit settings, `SimulateIo`, `UartPort`, `UartResetTool*`, password hash settings, and the `--hash-password` usage.
3. `COMMISSIONING.md`: a checklist with checkboxes covering camera index mapping, ROI-in-frame check, threshold calibration, PLC IP/rack/slot and DB1.DBB0 bit map, UART port/baud, the loss tests (unplug camera / Ethernet / USB-UART during idle and during a cycle → expect safe state), power-cycle restart, log location, and setting the service code. Mark every item "donanımda doğrulanmalı".
4. `KNOWN_ISSUES.md`: the remaining algorithm KNOWN-ISSUEs (functions 6/7, L/R overlay, hard-coded calibrations in 10/12), the camera index fragility, AForge being unmaintained, ClickOnce URL.
5. `RELEASE.md`: version bump in `AssemblyInfo.cs`, `build.ps1 -Configuration Release -Test`, manual smoke test in simulation mode, the hardware commissioning checklist, backing up `user.config` before an upgrade. ClickOnce publishing is an owner-only step.

## Acceptance criteria
- [ ] Each README claim in the Context list is corrected or verified against the code (cite file paths in the PR summary).
- [ ] The four new docs exist. COMMISSIONING items are checkboxes.
- [ ] `git grep -n "2580\|1903\|realtekno\"" -- docs README*` returns nothing (the brand name in prose is fine; no credentials).

## Verify (Windows, PowerShell)
```
git grep -n "2580\|1903" -- docs README.md README.tr.md
Get-ChildItem docs
```
