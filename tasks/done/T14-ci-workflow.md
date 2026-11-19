---
model: haiku
max_turns: 25
---

# T14 — CI workflow (Windows build + tests)

## Goal
Add a GitHub Actions workflow file that builds the solution and runs tests on `windows-latest`. Only create the file; enabling or pushing it is up to the owner.

## Context
- `build.ps1` (T02, `-Test` filled in T12).
- The solution uses packages.config and .NET Framework 4.7.2. `windows-latest` runners include VS Build Tools and vswhere.

## Scope
- Create `.github/workflows/build.yml` only.
- Do NOT push, do NOT add secrets, do NOT add publish/deploy/ClickOnce steps.

## Steps
1. Trigger on `push` and `pull_request` to `main`.
2. Job `build` on `windows-latest`: checkout, `microsoft/setup-msbuild@v2`, `NuGet/setup-nuget@v2`, then `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release -Test`.
3. Upload test results (`**/TestResults/*.trx`) as an artifact if produced.
4. Validate the YAML locally (e.g. `python -c "import yaml,sys; yaml.safe_load(open('.github/workflows/build.yml'))"` if Python is available; otherwise note "not validated").

## Acceptance criteria
- [ ] `.github/workflows/build.yml` exists, uses only official `actions/*`, `microsoft/*`, `NuGet/*` actions with pinned major versions.
- [ ] The workflow runs exactly the same `build.ps1` command developers use.
- [ ] No secrets or deploy steps.

## Verify (Windows, PowerShell)
```
python -c "import yaml; yaml.safe_load(open('.github/workflows/build.yml')); print('ok')"
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release -Test
```
