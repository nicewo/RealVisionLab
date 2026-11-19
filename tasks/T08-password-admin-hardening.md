---
model: sonnet
max_turns: 45
---

# T08 — Password and admin access hardening

## Goal
Remove hard-coded credentials from source, store the operator password as a salted hash, and require a password to open the settings screen. Existing installations must keep working.

## Context
- Hard-coded master code `2580`: `RealVisionLab/PasswordForm.cs:62,74`, `RealVisionLab/Property.cs:1473,1561`. Admin keyword `realtekno`: `Property.cs:1473,1561`.
- Default operator password `1903` in plaintext: `Properties/Settings.settings:15`, `App.config`. It is saved as plaintext at `PasswordForm.cs:40`.
- The settings screen opens without a password: `MainPage.cs:2465` `Pb_companyLogo_Click`.
- In Debug, `Property` forces `adminMode = true` (`Property.cs:78`). Keep this.

## Scope
- `PasswordForm.cs`, `Property.cs`, `MainPage.cs` (`Pb_companyLogo_Click`), settings files, a new `RealVisionLab/Security/PasswordHasher.cs`.
- Do NOT put any real secret value or its hash in the repo. Do NOT change `Program/` files.

## Steps
1. `PasswordHasher`: PBKDF2 (`Rfc2898DeriveBytes`, SHA256 if available on net472, else SHA1, 100k iterations, 16-byte salt). Format `v1$iter$saltB64$hashB64`. Constant-time compare.
2. New settings: `userPasswordHash` (string, default empty) and `serviceCodeHash` (string, default empty = service code disabled).
3. Migration: if `userPasswordHash` is empty and `userPassword` is non-empty, verify against the plaintext once. On success, write the hash and clear `userPassword`.
4. Remove the `2580` and `realtekno` literals. Admin unlock and password change use `serviceCodeHash` (disabled when empty). Document how the owner can generate a hash. Add a tiny `--hash-password` command-line mode in `Program.Main` that prints a hash to the console and exits.
5. `Pb_companyLogo_Click`: open `PasswordForm` first and open `Property` only on success (Debug builds may skip).
6. Fix `PasswordForm` validation: `Substring(4)` is evaluated before the length check (`:59`); reorder it.
7. Leave the `userPassword` default `1903` only as a migration source for existing configs. Change the new-install default to empty and show a "set password" prompt on first use.

## Acceptance criteria
- [ ] `git grep -n "\"2580\"\|\"realtekno\"\|\"1903\"" -- RealVisionLab` returns nothing.
- [ ] The settings screen requires a successful password in Release builds.
- [ ] A plaintext password from an old `user.config` still works once and is then migrated (unit test in T12 or a documented manual check).
- [ ] Debug build passes.

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "\"2580\"\|\"realtekno\"\|\"1903\"" -- RealVisionLab
```
