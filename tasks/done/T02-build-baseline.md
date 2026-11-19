---
model: sonnet
max_turns: 40
---

# T02 — Reproducible build baseline

## Goal
One command restores NuGet packages and builds the solution on a Windows dev machine. Record the real result.

## Context
- `RealVisionLab.sln`, `RealVisionLab/RealVisionLab.csproj`, `RealVisionLab/packages.config`
- Known issues (AUDIT §3): `packages/` is missing. `EnsureNuGetPackageBuildImports` (`csproj:271-276`) fails the build without restore. Absolute `System` HintPath (`csproj:126`). Duplicate `<Private>` elements (`csproj:135-136, 141-142, 146-147`). The `RealVision-Lab` configuration writes output into the tracked `..\Program\RealVision-Lab\` (`csproj:74`).
- MSBuild was not on PATH during the audit. `dotnet` SDK 10.0.404 is present but does not restore `packages.config`.

## Scope
- Create `build.ps1` at repo root. Edit `RealVisionLab.csproj` (minimal), create `docs/BUILD.md`.
- Do NOT: migrate to SDK-style csproj, change target framework, upgrade/remove packages, or build the `RealVision-Lab` configuration (it overwrites tracked files).

## Steps
1. `build.ps1` parameters: `-Configuration Debug|Release` (default Debug), `-Test` switch (no-op placeholder that T12 will fill).
   - Locate MSBuild with `vswhere.exe` (`${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`).
   - If MSBuild is not found, print a clear message telling the user to install VS 2022 Build Tools and the .NET Framework 4.7.2 Developer Pack, then exit with code 2.
   - Run `MSBuild RealVisionLab.sln -t:restore -p:RestorePackagesConfig=true`, then `MSBuild RealVisionLab.sln -p:Configuration=$Configuration -m -nologo -v:minimal`. Propagate the exit code.
2. In the csproj, replace the `System` reference with a plain `<Reference Include="System" />` and remove duplicate `<Private>` lines. Change nothing else.
3. Run `build.ps1`. If it fails for code reasons, fix only compile errors that block Debug/Release builds. Record each fix.
4. Write `docs/BUILD.md` (English): prerequisites, `build.ps1` usage, output path `RealVisionLab\Bin\<Config>\`, a warning not to use the `RealVision-Lab` configuration, and the actual result of your run (date, MSBuild version, warnings count, or the failure reason).

## Acceptance criteria
- [ ] `build.ps1` exists and exits 2 with a helpful message when MSBuild is missing.
- [ ] When MSBuild is available: Debug and Release builds succeed with 0 errors. Otherwise BUILD.md records "not verified: MSBuild missing".
- [ ] csproj diff touches only the `System` reference and duplicate `<Private>` lines (+ compile fixes, if any, listed in BUILD.md).
- [ ] No files under `Program/` are modified (`git status --short Program` empty).

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release
git status --short Program
```
