# RealVisionLab

**Camera-based industrial visual inspection with machine integration**

🇹🇷 [Türkçe README](README.tr.md)

RealVisionLab is a Windows application for industrial visual inspection. It captures images from multiple cameras, checks defined regions of interest (ROIs) and passes the result to the machine's automation.

It combines camera capture and image processing, Siemens S7 PLC communication and external I/O over UART in a single operator application.

## Key capabilities

- **Multi-camera capture:** lists video inputs via AForge.Video / DirectShow, camera selection and resolution setup.
- **ROI-based inspection:** rectangular inspection regions on the live image, per-region thresholds and visual pass/fail marking.
- **Image processing:** AForge.Imaging filter pipeline.
- **PLC integration:** Siemens S7 connection via Sharp7, writing result bits into DB areas.
- **External I/O:** commands to an output controller over a serial port (UART).
- **Barcode / QR:** ZXing and ZBar for code-reading workflows.
- **Operator and settings screens:** Windows Forms UI for cameras, ROIs, thresholds, PLC and UART configuration, behind a password screen.
- **Logging:** daily scan logs in date-based text files, plus diagnostic output.

## Tech stack

| Layer | Technology |
|---|---|
| Application | C# / Windows Forms |
| Runtime | .NET Framework 4.7.2 |
| Camera | AForge.Video, DirectShow |
| Image processing | AForge.Imaging |
| PLC | Sharp7 / Siemens S7 |
| Serial | System.IO.Ports |
| Barcode / QR | ZXing.Net, ZBar |
| Configuration | .NET Application Settings |

## Solution layout

- `RealVisionLab.sln` — Visual Studio solution
- `RealVisionLab/RealVisionLab.csproj` — .NET Framework 4.7.2 Windows app
- `RealVisionLab/MainPage.cs` — main operator screen, camera and inspection flow
- `RealVisionLab/Property.cs` — configuration UI
- `RealVisionLab/PasswordForm.cs` — password screen
- `RealVisionLab/Message.cs` — message screen
- `RealVisionLab/Settings1.cs` — settings helper
- `Program/` — published ClickOnce setup

## Development environment

- Windows, Visual Studio, .NET Framework 4.7.2 Developer Pack
- Internet access for NuGet restore
- A DirectShow-visible camera (or a virtual camera) for testing
- PLC and UART hardware, or a test rig, for those features

Open the solution, restore NuGet packages and build the `RealVisionLab` project.

## Commissioning notes

1. Configure camera indexes and resolutions for the real camera list on the target PC.
2. Make sure ROI coordinates match the camera resolution and display scaling.
3. Calibrate thresholds with real product samples under varying light, exposure and product variants.
4. Check PLC IP, rack/slot and DB addresses against the machine's PLC program.
5. Verify the UART port, baud rate and the I/O device's protocol.
6. Test camera, PLC and serial loss scenarios before enabling machine outputs.

## Safety and reliability

- PLC outputs can affect machine motion and process safety. Safety functions must not rely on this PC application; design and validate them independently at PLC and hardware level.
- Verify on the real machine that the system goes to a safe state on camera or communication failure.
- The app uses .NET Framework 4.7.2 and AForge; test compatibility with newer Windows versions and hardware.

## Status

Source code of a production Windows Forms application. A modern .NET rewrite is in progress in a separate (private) repository.

---

**RealVisionLab — Vision inspection meets industrial control.**
