# Build

## Prerequisites (Windows)
- Visual Studio 2022 Build Tools (or full VS 2022) with the MSBuild component
- .NET Framework 4.7.2 Developer Pack
- Internet access for NuGet restore (`packages.config`)

`dotnet` CLI alone is not sufficient: it does not restore `packages.config` projects.

## Usage
```powershell
.\build.ps1                          # Debug
.\build.ps1 -Configuration Release
.\build.ps1 -Test                    # placeholder, tests are added in T12
```
The script locates MSBuild via `vswhere`, runs a `packages.config` restore, then builds `RealVisionLab.sln`.
If MSBuild is missing it prints install instructions and exits with code 2. Other exit codes come from MSBuild.

## Output
`RealVisionLab\Bin\<Config>\`

## Warning
**Do not use the `RealVision-Lab` configuration.** It writes into the tracked `Program\RealVision-Lab\` folder and overwrites committed files.
`build.ps1` only accepts `Debug` and `Release`.

## csproj changes (T02)
- `System` reference: replaced the absolute-path HintPath with `<Reference Include="System" />`.
- Removed duplicate `<Private>True</Private>` lines (System.Drawing.Primitives, System.Runtime, System.Runtime.InteropServices.RuntimeInformation).
- No compile fixes were made (build not run).

## Result of the baseline run
- Date: 2026-11-19
- Status: **not verified: build not executed.** The agent session's permission policy blocked running `build.ps1`, so no MSBuild version, warning count, or error count is available. MSBuild availability on this machine is also unconfirmed.
- To close this: run the commands below on a dev machine and record the result here.
```powershell
.\build.ps1 -Configuration Debug
.\build.ps1 -Configuration Release
git status --short Program
```
