---
model: opus
max_turns: 60
---

# T06 — Thread safety: frame hand-off and UI marshaling

## Goal
Remove cross-thread UI access and image races so that `CheckForIllegalCrossThreadCalls = false` can be deleted.

## Context
- `MainPage.cs:138` `CheckForIllegalCrossThreadCalls = false`.
- `New_Frame` (`:669-725`) runs on the AForge camera thread and sets/disposes `pb_scene.Image`. `Islemler` reads `pb_scene.Image` (`:897`) and also assigns it (`:976, 1039, 1110`), so it can be disposed by the camera thread mid-use.
- Inspection functions run inside `Task.Run` (`:932`) and write `tx_debug`, `lbl_alarm`, `txt_qr`, `RotUzunluk`, `pb_scene.Image`.
- `Btn_bekle` (`:2214`) uses `Thread.Sleep` loops on the thread pool. `Qr_Oku_harici` (`:2269`) calls `Application.DoEvents()` from the thread pool and reads `txt_qr.Text`.
- `Tx_debug_TextChanged` appends to `debug.txt` on every change (`:2487`).

## Scope
- `MainPage.cs` (+ a small new helper file if useful, e.g. `RealVisionLab/FrameBuffer.cs`).
- Do NOT change inspection math or cycle timing values.

## Steps
1. Add a `FrameBuffer`: the camera thread stores a clone of the latest frame per camera under a lock. `GetLatest(cameraIndex)` returns a new clone. The display is updated via `BeginInvoke` and disposes the previous display image on the UI thread only.
2. `Islemler` takes frames from `FrameBuffer` (not from `pb_scene.Image`). It keeps the bounded wait introduced in T03.
3. Add `UiLog(string)` and `SetAlarm(string, Color)` helpers that marshal with `BeginInvoke` if `InvokeRequired`. Replace all UI writes inside inspection functions with these helpers.
4. `Qr_Oku_harici`: replace `Thread.Sleep` + `DoEvents` with an `async` wait on a `TaskCompletionSource<string>` completed by the key handler (`password_KeyDown`), with the same timeout (`Bekleme[9]` × 100 ms).
5. `Btn_bekle`: make it async with `await Task.Delay(100)` and use `IMachineIo` (T04) for reads/writes.
6. Remove `CheckForIllegalCrossThreadCalls = false` and run the app in Debug with `SimulateIo=true` and an image file (`sec` button) to confirm no `InvalidOperationException` cross-thread errors.

## Acceptance criteria
- [ ] `CheckForIllegalCrossThreadCalls` no longer appears in the code.
- [ ] `Application.DoEvents` no longer appears in `MainPage.cs`.
- [ ] `Islemler` does not read `pb_scene.Image` to get inspection input.
- [ ] Debug build passes. A manual Debug run (or the T13 test) shows no cross-thread exception in `debug.txt`. If it cannot be run, record "not run" explicitly.
- [ ] Summary notes: "donanımda doğrulanmadı (gerçek kamerayla test edilmedi)".

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "CheckForIllegalCrossThreadCalls\|Application.DoEvents" -- RealVisionLab
```
