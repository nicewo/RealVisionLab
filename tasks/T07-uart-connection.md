---
model: sonnet
max_turns: 40
---

# T07 — UART connection robustness

## Goal
The UART connects to the configured port with timeouts, never writes to a closed port, does not block the UI thread, and only launches the external reset tool when explicitly enabled.

## Context
- `RealVisionLab/MainPage.cs` `Uart_connect` (`:489-615`):
  - always picks `ports[portscount - 1]` (last COM port) (`:506`)
  - `finally` writes even if `Open()` failed (`:544-550`)
  - `Thread.Sleep(1000)` on the UI thread (`:551`)
  - launches `C:\UartReset\UartReset.exe` with `Verb = "runas"` (`:519-526, 557-567`)
- The `Uart` setting ("3-3") is unused. `Property.cs:1297` shows the last port in `txt_uart_com` but never saves it.
- After T04, writes go through `UartMachineIo`.

## Scope
- `MainPage.cs` (`Uart_connect`, timer reconnect), `Property.cs` / `Property.Designer.cs` (port field save), `Properties/Settings.settings` + `Settings.Designer.cs` + `App.config` (new settings), `Io/UartMachineIo.cs`.
- Do NOT change the 1-byte protocol or bit mapping.

## Steps
1. Add settings `UartPort` (string, default empty = legacy behaviour: last port, with a log warning), `UartResetToolEnabled` (bool, default False), `UartResetToolPath` (string, default `C:\UartReset\UartReset.exe`).
2. The Property screen saves `txt_uart_com` into `UartPort` in `button2_Click`. Turn `txt_uart_com` into a ComboBox of `SerialPort.GetPortNames()` if that is simple. Otherwise validate the text.
3. Set `ReadTimeout`/`WriteTimeout` = 500 ms on the port.
4. Restructure the connect routine: try Open → on success attach `DataReceived`, write 0x00, mark connected. On failure, mark disconnected and do not write. Remove the UI-thread `Thread.Sleep(1000)`.
5. Launch the reset tool only if `UartResetToolEnabled` and the file exists. Log each launch.
6. `Uart_to_IO_DataReceived`: guard against empty `ReadExisting()` (`recieveText[0]` currently throws on empty input).

## Acceptance criteria
- [ ] No `ports[portscount - 1]` without first checking `UartPort` is empty.
- [ ] No write path executes after a failed `Open()` (code review).
- [ ] `runas` launch only happens behind `UartResetToolEnabled`.
- [ ] `Thread.Sleep` no longer appears in `Uart_connect`.
- [ ] Existing installs keep working: an empty `UartPort` falls back to the legacy behaviour.
- [ ] Debug build passes. Summary notes: "donanımda doğrulanmadı".

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "runas\|Thread.Sleep" -- RealVisionLab/MainPage.cs
```
