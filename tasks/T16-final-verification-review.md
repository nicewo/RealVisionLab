---
model: opus
max_turns: 70
---

# T16 — Final verification review

## Goal
Independently verify every acceptance criterion of T01–T15 against the actual code. Run the build and tests, fix small gaps, and update the plan checklist only for verified items.

## Context
- `tasks/T01-*.md` … `tasks/T15-*.md` (acceptance criteria), plus `tasks/done/` if tasks were moved there.
- `docs/AUDIT.md` (original findings), `docs/CLAUDE_DEVELOPMENT_PLAN.md` (checklist at "## Checklist").
- `build.ps1`.

## Scope
- Read-and-verify everything. Small fixes are allowed: under ~30 changed lines each, no behaviour change to output sequencing or measurement math.
- Larger gaps: do not fix. List them in the report as follow-up tasks (`tasks/TNN-*.md` numbered after the highest existing one, using the same front-matter format).
- Do NOT commit, push, deploy, or touch hardware.

## Steps
1. Run `build.ps1 -Configuration Debug -Test` and `-Configuration Release -Test`. Record exact output summaries.
2. For each task T01–T15, check every acceptance checkbox with evidence (file:line, grep output, or test name). Mark it ✅ verified / ❌ not met / ⚠️ not verifiable (e.g. hardware).
3. Re-check AUDIT §4.1 and §4.2 critical findings one by one. State which are resolved.
4. Fix small gaps. Re-run build/tests after fixes.
5. Write `docs/VERIFICATION.md` (Turkish): the per-task table, commands + results, remaining risks, and the "donanımda doğrulanmadı" list.
6. Update `docs/CLAUDE_DEVELOPMENT_PLAN.md` "## Checklist": tick `Audit` (AUDIT.md exists), and tick `Hygiene baseline`, `Milestones verified`, `Release docs` only if their related criteria are all ✅. Never tick `Roadmap approved`; that is the owner's call. Change nothing else in the plan.

## Acceptance criteria
- [ ] `docs/VERIFICATION.md` exists and covers every acceptance item of T01–T15 with evidence.
- [ ] Build/test commands were actually run, and their real output is quoted (or the failure reason).
- [ ] The plan checklist only ticks items backed by ✅ evidence. `Roadmap approved` remains unticked.
- [ ] Follow-up task files exist for every ❌ item that was not fixed.

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug -Test
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Release -Test
git diff --stat
```
