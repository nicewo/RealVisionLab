---
model: opus
max_turns: 50
---

# T05 — Safe outputs and unintended actuation

## Goal
Outputs go to a defined safe state (all off) on communication/camera loss, crash and shutdown. UI gestures can no longer start the machine cycle or wipe the configuration by accident.

## Context
- Depends on T04 (`IMachineIo.ForceSafeState`).
- `MainPage.cs:2459` `pb_RealTekno_Click` sets `buton = true`, so clicking the logo starts a cycle and drives Lamba + Piston.
- `MainPage.Designer.cs:166-175` + `MainPage.cs:2460-2463`: a hidden 5×5 px label calls `Settings.Default.Reset()` without confirmation.
- `Program.cs:24-40`: ThreadException → `Application.Restart()` with no output reset, no restart rate limit, and no `AppDomain.CurrentDomain.UnhandledException`.
- `MainPage_FormClosing` (`MainPage.cs:2447`) does not reset outputs.
- Cycle start is accepted even if a camera was not found (`Cam_adjust` `:418-423`).

## Scope
- `Program.cs`, `MainPage.cs`, `MainPage.Designer.cs` (only the `Reset_setting` wiring), new setting(s) if needed.
- Do NOT change output sequencing of a normal OK/NOK cycle.

## Steps
1. `pb_RealTekno_Click`: start a cycle only when `debugMode` is true or `SimulateIo` is true. Otherwise ignore the click.
2. `Reset_setting`: remove the click handler wiring. Settings reset is only reachable from the Property screen in admin mode, behind a Yes/No confirmation, and it makes a backup copy of current Rect/Cam/Bekleme strings to `settings-backup-<timestamp>.txt` next to the exe first.
3. Gate cycle start (`buton` handling in `Main_timer_Tick` `:754`): if any configured camera is not running, or an enabled I/O backend is disconnected, refuse to start, call `io.ForceSafeState()`, and show the reason in `lbl_alarm`.
4. Call `io.ForceSafeState()` in: `MainPage_FormClosing`, the `Islemler` catch block, `Program` ThreadException handler, and a new `AppDomain.CurrentDomain.UnhandledException` handler (log + safe state).
5. Restart loop guard: write the restart timestamp to `%LOCALAPPDATA%\RealVisionLab\restarts.txt`. If 3 restarts happen within 60 s, do not restart; show a MessageBox and exit after the safe state.
6. When the PLC or UART connection drops during a cycle (detected in the timer), abort the cycle: mark NOK and force safe state.

## Acceptance criteria
- [ ] Release build: clicking the logo does not set `buton` (code-verifiable branch).
- [ ] `Settings.Default.Reset()` is only called after confirmation + backup.
- [ ] `ForceSafeState` is invoked in FormClosing, the Islemler catch, ThreadException and UnhandledException (grep).
- [ ] Restart guard is implemented with a 3-in-60s limit.
- [ ] Debug build passes. With `SimulateIo=true`, `SimulatedMachineIo` records an all-false write on close (manual check or T13 test).
- [ ] Summary notes: "donanımda doğrulanmadı". Real machine fail-safe verification is listed for the user.

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "ForceSafeState" -- RealVisionLab
git grep -n "Settings.Default.Reset()" -- RealVisionLab
```
