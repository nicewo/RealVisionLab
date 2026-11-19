---
model: sonnet
max_turns: 40
---

# T10 — Resource leaks and log rotation

## Goal
Bound memory and disk usage during long production runs.

## Context
- `RealVisionLab/MainPage.cs`:
  - `Bitmap[] bitmap` per cycle is never disposed (`:851`, `:897`).
  - `Pen` objects are not disposed (`:973, 1036, 1097`).
  - `Kırmızı_say` replaces `bmp[r_n]` without disposing the input (`:1247, 1291`).
  - `Threshold_Apply` allocates a new Bitmap per frame and never disposes the previous `pb_scene.Image` (`:2357, 2432`).
  - `Qr_Okuyucu` allocates `bwBitmap` per call (`:2184`).
  - `debug.txt` is appended on every `tx_debug` change with no size limit (`:2487-2503`, `Program.cs:30`). Relative paths depend on the working directory.
  - The daily scan log rolls over only once (`_2.txt`) at 1 GB (`:1133-1146`).
- The field log shows `OutOfMemoryException` at `Bitmap.Clone` (`Program/RealVision-Lab/debug.txt`).

## Scope
- `MainPage.cs`, `Program.cs`, optional new `RealVisionLab/Logging/FileLog.cs`.
- Do NOT change the scan log line format (`Kamera N -scan,roi-<ms>ms,<hata>,<min>,<val>,<max>/`).

## Steps
1. Dispose per-cycle bitmaps at the end of `Islemler` (in a `finally`), except the one currently shown, which is disposed when replaced. Use `using` for Pens/Graphics.
2. `Threshold_Apply`: dispose the previous display image when replacing it (on the UI thread if T06 is done).
3. `FileLog`: absolute directory = `AppDomain.CurrentDomain.BaseDirectory` (keep the file name `debug.txt`). Size-based rotation at 10 MB, keep 5 files. Thread-safe. Replace both `debug.txt` writers.
4. Scan log: keep the `Values/yyyy_MM_dd.txt` naming, resolve it against the base directory, and roll to `_2`, `_3`, … at 100 MB.
5. Add a Debug-only memory counter in `lbl_islemTime` or `debug.txt` (`GC.GetTotalMemory`, `Process.PrivateMemorySize64`) every 100 cycles.

## Acceptance criteria
- [ ] Every `new Pen(` in `MainPage.cs` is inside a `using`.
- [ ] `File.AppendText("debug.txt")` is no longer present; all diagnostics go through `FileLog`.
- [ ] Log files are resolved against the application base directory.
- [ ] Debug build passes. If T13 exists, a 200-cycle simulated run shows no monotonic growth of `PrivateMemorySize64` > 50 MB (otherwise record "not measured").

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "AppendText(\"debug.txt\")\|new Pen(" -- RealVisionLab
```
