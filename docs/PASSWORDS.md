# Passwords and service code (T08)

- Operator password: stored as `userPasswordHash` (`v1$iterations$salt$hash`, PBKDF2-HMAC-SHA256, 100k iterations). Required to open the settings screen in Release builds (Debug skips it).
- New installs have no password: the password form asks for a new 4-digit password on first use.
- Migration: an old `user.config` with plaintext `userPassword` still works once; on a successful entry the hash is written and `userPassword` is cleared. (An old install that never saved its settings has no stored password and gets the first-use prompt; the former built-in default is gone.)
- Service code (admin unlock in the settings screen "send" box, and password reset in the password form with `<code>*+`): verified against `serviceCodeHash`. Empty = disabled. There is no built-in code.
- Changing the password: enter `<current password>*+`, then the new one.

## Generating a service code hash

```
RealVisionLab.exe --hash-password "<your secret>"
```

Prints a `v1$...` line to the console and exits (run from cmd/PowerShell). Put the value into `serviceCodeHash` in the machine's `user.config`. Never commit the secret or its hash to the repository.
