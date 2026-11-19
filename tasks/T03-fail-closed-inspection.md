---
model: opus
max_turns: 50
---

# T03 — Fail-closed inspection and ROI bounds

## Goal
Any failure while inspecting an ROI must mark that ROI as NOK (fail-closed). A missing frame or an invalid ROI must never hang the cycle or produce an "ONAYLANDI".

## Context
- `RealVisionLab/MainPage.cs`
  - `Reset()` sets every `hata[c,i] = false` (`:443-449`).
  - Functions 0-11 catch exceptions and only log; `hata` is not set (e.g. `Kırmızı_say` `:1293`, `Maks_Genislik` `:1338`).
  - `Cizgi_Ile_Lokasyon_Bul` (`:1712`) never assigns `hata`.
  - Frame-acquisition loop `do { … } while (bitmap[c] == null)` with no timeout (`:891-902`).
  - `bitmap[c].Clone(Rect[i], PixelFormat.Format24bppRgb)` (`:931`) throws GDI+ OutOfMemoryException for out-of-frame ROIs (field log `Program/RealVision-Lab/debug.txt`, 29.07.2024).
  - `Btn_bekle` reads PLC bit `0` hard-coded (`:2249`) instead of `Settings.Default.plc_buton`.
  - `Qr_Okuyucu`/`Qr_Oku_harici`: `find_text.Split('-')[1]` throws when the part name has no `-` (`:2202, 2295`).
  - `Iki_method_arasi_fark` (`:2168`) uses ROI width/height as array indices without bounds check.

## Scope
- Change only `MainPage.cs`.
- Do NOT change measurement math, thresholds, or the meaning of any setting. Do NOT refactor the dispatch (T11 does that).

## Steps
1. At the start of each ROI evaluation in `Islemler`, set `hata[c,i] = true` (pessimistic). Each function sets it to `false` only on a successful in-range result. For function 9 (`Cizgi_Ile_Lokasyon_Bul`, a locator) and 11 (`Audi_alttan_uzat`, a helper), set `hata = false` only on successful completion. Document this choice in a code comment.
2. In every catch block of functions 0-20, set `hata[c_n, r_n] = true` before logging.
3. Before `Clone`, intersect `Rect[i]` with the frame bounds. If the intersection is empty or differs from `Rect[i]`, mark the ROI NOK, log `"ROI {i} out of frame"`, and skip the function.
4. Replace the unbounded frame loop with a bounded one: max wait = `Bekleme[1]` × 5 or 5000 ms, whichever is larger. On timeout, mark all ROIs of that camera NOK, set `lbl_alarm` to `"Kamera {c+1} görüntü yok"`, and continue with the existing NOK path.
5. `Btn_bekle`: use `Settings.Default.plc_buton`.
6. QR functions: if the part name has no `-`, treat it as NOK with a log line instead of throwing.
7. `Iki_method_arasi_fark`: validate indices `< Ofset.GetLength(0)`; otherwise NOK.

## Acceptance criteria
- [ ] Every catch block in tarama functions 0-20 sets `hata[c_n, r_n] = true` (grep-verifiable).
- [ ] No `while (bitmap[c] == null)` loop without a deadline remains.
- [ ] `Clone(Rect[i]` is preceded by a bounds check.
- [ ] `S7.GetBitAt(controlPLC, 0, 0)` no longer appears; `plc_buton` is used.
- [ ] `build.ps1` Debug build passes.
- [ ] Summary notes: "donanımda doğrulanmadı".

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "while\s*(bitmap\[c\] == null)" -- RealVisionLab/MainPage.cs
git grep -n "GetBitAt(controlPLC, 0, 0)" -- RealVisionLab/MainPage.cs
```
(both greps must return nothing)
