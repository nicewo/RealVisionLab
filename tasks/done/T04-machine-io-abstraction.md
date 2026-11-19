---
model: opus
max_turns: 60
---

# T04 — Machine I/O abstraction (S7 / UART / Simulated)

## Goal
All PLC and UART reads/writes go through one thread-safe interface with a simulated implementation. This allows safe-state logic (T05) and cycle tests (T13) without hardware.

## Context
- `RealVisionLab/MainPage.cs`: S7 writes are duplicated at `:462-475, 589-612, 866-879, 1179-1193, 1203-1218, 2219-2229, 2246-2263, 2274-2284`; S7 reads at `:822, 2249`. UART writes at `:455, 546, 578, 863, 1177, 1201`. UART input decode at `:2504-2549`. `ConvertToByte` at `:2308`.
- `S7Client` is used concurrently from the UI timer and `Task.Run` without a lock (AUDIT §4.2).
- Current output semantics (preserve exactly): logical outputs `Lamba`, `Piston`, `Kase`, `Bos` map to UART bit indices `Settings.Lamba/Piston/Kase/bosBit` in a 1-byte mask, and to bits of `DB1.DBB0` via `plc_lamba/plc_piston/plc_kase/plc_bos`. Inputs: start (`buton` / `plc_buton`) and reset (`reset_buton`).

## Scope
- Add new files under `RealVisionLab/Io/`: `IMachineIo.cs`, `MachineOutputs.cs` (struct/flags), `S7MachineIo.cs`, `UartMachineIo.cs`, `CompositeMachineIo.cs`, `SimulatedMachineIo.cs`. Register them in the csproj.
- Replace direct output/input calls in `MainPage.cs` with the interface. Keep UART connect/reconnect logic in place for now (T07 owns it); just route writes through the interface.
- Do NOT change bit mappings, DB numbers, timing, or the UI.

## Steps
1. `IMachineIo`: `bool IsConnected`, `bool TryWriteOutputs(MachineOutputs o, out string error)`, `bool TryReadInputs(out MachineInputs i, out string error)`, `void ForceSafeState()` (all four outputs false, best effort, never throws).
2. `S7MachineIo` wraps `S7Client` with a private `lock`. It does read-modify-write on DB1.DBB0 exactly as today (DB 1, start 0, size 1).
3. `UartMachineIo` wraps `SerialPort` with a lock and builds the byte with the existing `ConvertToByte` logic (move it to a static helper).
4. `CompositeMachineIo` fans out to the enabled backends (`PlcEnable`, `UartEnable`).
5. `SimulatedMachineIo` records every write (timestamped list) and lets tests inject inputs.
6. Add a setting `SimulateIo` (bool, default False). When true, MainPage uses `SimulatedMachineIo` and shows "SİMÜLASYON" in `port_text`.
7. Replace all direct `client.DBWrite`/`Uart_to_IO.Write` output calls in MainPage with `io.TryWriteOutputs(...)`. Errors go to `port_text` as today.

## Acceptance criteria
- [ ] `git grep -n "DBWrite\|Uart_to_IO.Write" RealVisionLab/MainPage.cs` returns no output-writing call sites (connect probes inside `Uart_connect` may remain, with a comment).
- [ ] The S7 client is only touched inside `S7MachineIo` (plus `ConnectTo` in the connect routine) and every call is under its lock.
- [ ] Bit mapping is unchanged (reviewer can diff old vs new mapping table in the summary).
- [ ] Debug build passes.
- [ ] Summary notes: "donanımda doğrulanmadı".

## Verify (Windows, PowerShell)
```
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Configuration Debug
git grep -n "DBWrite" -- RealVisionLab
```
