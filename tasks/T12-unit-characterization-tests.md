---
model: sonnet
max_turns: 55
---

# T12 — Unit and characterization tests

## Goal
Add a hardware-independent test project that pins current behaviour of config parsing, helpers and all inspection algorithms.

## Context
- `RealVisionLab.Core` (T11), `Config/ConfigParser` (T09), `PasswordHasher` (T08), `ConvertToByte`, `TransformPoint`.
- Default config strings: `RealVisionLab/Properties/Settings.settings`.
- Real-world log sample: `degerler.txt` (scan log format).

## Scope
- New `RealVisionLab.Tests/RealVisionLab.Tests.csproj`: SDK-style, `net472`, MSTest or NUnit, with `Microsoft.NETFramework.ReferenceAssemblies` so `dotnet test` works without the targeting pack. Add it to the sln.
- Fill the `-Test` switch in `build.ps1` (run `dotnet test RealVisionLab.Tests` or `vstest.console`, whichever works; document which).
- Do NOT change production code except to fix a bug a test exposes in T08/T09 code. Algorithm KNOWN-ISSUEs are asserted as-is.

## Steps
1. ConfigParser: round-trip the defaults; reject invalid records (bad ints, W=0, Islem=21, Threshold=300, CamId out of range); handle an empty string and trailing `/`.
2. `ConvertToByte`: all bit positions, short arrays (padding).
3. `TransformPoint`: identity, pure translation, 90° rotation, scale 2.
4. PasswordHasher: verify ok/fail, plaintext migration path, malformed hash string.
5. Algorithms: synthetic bitmaps generated in code (no binary fixtures). At least one pass and one fail case per function 0-8, 10, 12-18. Plus a "throws/invalid input gives NOK" case for each (fail-closed contract from T03). Characterize functions 6/7 current outputs with `// KNOWN-ISSUE` comments.
6. Scan-log line format: build a line the way `Islemler` does and parse it with the logic from `MainPage_Load`/`Property.Dosya_goster`. Include the first 3 records of `degerler.txt` as inline strings.

## Acceptance criteria
- [ ] `build.ps1 -Test` runs all tests, exits 0, and prints the pass count.
- [ ] Coverage of the list above: each numbered step has ≥ 1 test class.
- [ ] No test needs a camera, a PLC, a COM port, or network access.

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug -Test
```
