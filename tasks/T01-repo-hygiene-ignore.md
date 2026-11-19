---
model: haiku
max_turns: 20
---

# T01 — Repo hygiene: ignore rules and attributes

## Goal
Stop new build artifacts, NuGet packages, runtime logs and local machine config from entering git, without deleting or untracking anything already tracked.

## Context
- `.gitignore` (currently has an uncommitted `logs/` addition — keep it)
- `.gitattributes` (only `* text=auto`)
- Runtime-created files (see `docs/AUDIT.md` §4.5): `debug.txt` (`RealVisionLab/MainPage.cs:2493`, `RealVisionLab/Program.cs:30`), `Values/` (`MainPage.cs:116`), `CameraSettings.xml` (`MainPage.cs:2697`), `packages/` (NuGet packages.config restore), `RealVisionLab_TemporaryKey.pfx` (`RealVisionLab.csproj:89`), `app.publish/`.

## Scope
- Change: `.gitignore`, `.gitattributes` only.
- Do NOT: run `git rm`, `git rm --cached`, commit, or touch anything under `Program/`, `degerler.txt`, source files. Untracking existing files is a user decision (AUDIT §8 item 4).

## Steps
1. Add to `.gitignore`: `packages/`, `*.nupkg`, `debug.txt`, `**/Values/`, `CameraSettings.xml`, `*.pfx`, `*.snk`, `app.publish/`, `user.config`. Keep existing entries and `logs/`.
2. In `.gitattributes`, keep `* text=auto`. Add `*.cs text eol=crlf`, `*.csproj text eol=crlf`, `*.sln text eol=crlf`, `*.resx text eol=crlf`, `*.config text eol=crlf`, `*.settings text eol=crlf`, and mark `*.dll *.exe *.png *.jpg *.ico *.deploy binary`.
3. Run `git status --short` and `git check-ignore -v packages/x debug.txt Values/a.txt CameraSettings.xml` to confirm the rules work.
4. Add a short "Tracked artifacts still present" note to the bottom of `.gitignore` as a comment that lists `Program/`, `degerler.txt`, `Program/RealVision-Lab/debug.txt` and says the owner should decide on untracking them.

## Acceptance criteria
- [ ] `git check-ignore` matches each of: `packages/x`, `debug.txt`, `RealVisionLab/Values/a.txt`, `CameraSettings.xml`, `x.pfx`.
- [ ] No tracked file is deleted or untracked (`git status --short` shows only `.gitignore`/`.gitattributes` modified).
- [ ] `git ls-files --eol RealVisionLab/MainPage.cs` runs without error (renormalization is NOT performed).

## Verify (Windows, PowerShell)
```
git check-ignore -v packages/x debug.txt RealVisionLab/Values/a.txt CameraSettings.xml x.pfx
git status --short
```
