# TODO

Target machine: Lubuntu 25.10, X11, spectrwm, keyd, 2560x1600 panel, Avalonia scale 2.25. Log file on the target: `~/.local/state/xerahs/Logs/2026-09/XerahS-20260921.log`. Current deployment: `~/Applications/XerahS-0.28.32-linux-x64.AppImage` run by `~/.config/systemd/user/xerahs.service` (Restart=always, `XERAHS_LINUX_HOTKEY_BACKEND=x11`).

## 1. Build and verify the X11 capture fix

- [x] Get a .NET 10 SDK: the box's system `dotnet` (`/usr/bin/dotnet`, SDK 10.0.112).
- [x] Build `src/desktop/app/XerahS.App` for `linux-x64`, Release: `XERAHS_ARCHITECTURES=linux-x64 bash build/linux/package-linux.sh` wrote the 0.28.35 AppImage, .deb, and tarball to `dist/`. The fixed `XerahS.Platform.Linux.dll` (no `SetPixel` reference) is embedded byte for byte in the packaged `XerahS` executable, identical across all three packages. The AppImage starts under Xvfb without an unhandled exception.
- [x] Confirm `X11ScreenCapture.CaptureFullScreen` output matches the old code pixel for pixel. On Xvfb at 2560x1600 showing a static noise image, old (upstream `1f4249d`) and new output is byte-identical (16,384,000 bytes) at depth 24 (the BGRX row-copy path) and at depth 30 (the per-pixel fallback, masks 0x3ff00000/0xffc00/0x3ff). At depth 24 both match an ImageMagick `import` of the same screen with 0 differing pixels; at depth 30 both recover the source image exactly, where `import` rounds one level low. Harness: `/workspace/scratch/xerahs-captest/run.sh`.
- [x] Measure: log milestone `region capture UI invoked (+N ms)` for the RectangleRegion job. Old value 3111 to 7321 ms. Packaged AppImage on Xvfb at 2560x1600, Ctrl+Shift+X sent through XTest, pre-capture by the `x11` provider: +109, +80, +78 ms, overlay displayed by +303 ms. The capture call alone: 13 ms median new, 4093 ms old, same screen.
- [ ] Point `xerahs.service` `ExecStart` at the built binary on the target machine, then read the milestone from the target log.

## 2. Capture only the selected rectangle on X11

`LinuxScreenCaptureService.CaptureRectAsync` captures the full screen and crops (lines 298 to 302). Call XGetImage on the rectangle itself.

## 3. Hotkeys under keyd

`InputDeviceEnumerator.cs:99` keeps only keyboards that are readable and non-virtual. keyd grabs the physical keyboard (`EVIOCGRAB`), so the evdev listener on `/dev/input/event3` receives nothing and the keyd virtual keyboard (`event10`, name "keyd virtual keyboard") is skipped.

- [ ] On X11, default to the XGrabKey backend (`LinuxHotkeyService`), or
- [ ] detect a grabbed device (`EVIOCGRAB` test returns EBUSY) and fall back to virtual keyboards / X11.
- [ ] Remove the need for `XERAHS_LINUX_HOTKEY_BACKEND=x11`.

## 4. Region overlay under tiling window managers

The overlay (`XerahS.RegionCapture/UI/OverlayWindow.axaml`: Topmost, CanResize=False, title "XerahS Region Capture Overlay") is an ordinary managed window, so spectrwm tiles it. The target machine carries a workaround in `~/.spectrwm.conf`: `quirk[.*:.*:XerahS Region Capture Overlay] = FLOAT + ANYWHERE + FULLSCREEN`.

- [ ] On X11, set `_NET_WM_STATE_FULLSCREEN` on the overlay before map, or make it override-redirect.
- [ ] Test on spectrwm with the quirk removed.

## 5. Clipboard crash

Unhandled exception in the main loop, process exits:

```
System.ObjectDisposedException: Cannot access a disposed object.
Object name: 'Ref<Avalonia.Platform.IBitmapImpl>'.
   at Avalonia.Utilities.RefCountable.Ref`1.get_Item()
   at Avalonia.Media.Imaging.Bitmap.Save(Stream stream, BitmapEncoderOptions options)
   at Avalonia.X11.Selections.SelectionDataProvider.TryGetDataAsBytes(...)
   at Avalonia.X11.Selections.SelectionDataProvider.OnSelectionRequest(XSelectionRequestEvent& request)
   at Avalonia.X11.Selections.Clipboard.X11ClipboardImpl.OnEvent(XEvent& evt)
```

XerahS puts an Avalonia `Bitmap` on the X11 clipboard and disposes it while it still owns the selection. A later paste in another app requests the data and hits the disposed bitmap.

- [ ] Find the clipboard-set call in `LinuxClipboardService` and keep the bitmap alive (or hand over encoded PNG bytes) until selection ownership is lost.
- [ ] The catch-all in `Program.Main` labels every unhandled exception "unexpected startup failure"; label runtime crashes as runtime crashes.

## 6. Packaging

- [x] Produce a linux-x64 AppImage or self-contained publish folder from the fork: `dist/XerahS-0.28.35-linux-x64.AppImage`.
- [ ] Rebase on `upstream/develop` (the upstream default branch) when upstream moves. The fork is Linux only (see `FORK.md`), so expect conflicts wherever upstream edits the deleted Windows, macOS, or mobile files or the Windows branches of shared code.
- [ ] The update checker (`GitHubUpdateChecker`) looks at `ShareX/XerahS` releases, which lack the fork's changes. Point it at the fork's releases or turn update checks off.

## Upstream pull requests

Each branch is 1 commit on `upstream/develop` `1f4249d`. `/workspace/deliver/fuck-github.sh` re-authors the commits on the host, pushes the branches to `origin`, and opens the pull requests with the bodies in `/workspace/deliver/pr-bodies/xerahs-*.md`.

- [ ] `fix/tests-non-windows-build`, open as ShareX/XerahS#286, awaiting review: `WindowsModernCaptureServiceTests.cs` calls `WindowsModernCaptureService`, which is not compiled off Windows, so `dotnet build src/desktop/XerahS.sln` fails on Linux with 2 errors (CS0103). The branch puts the 2 `ShouldUseModernCapture_*` tests under `#if WINDOWS`.
- [ ] `fix/x11-capture-row-copy`, open as ShareX/XerahS#287, awaiting review: the row-copy capture, with the conversion moved into `X11ScreenCapture.ConvertZPixmap` and 7 NUnit tests in `tests/XerahS.Tests/Platform/Linux/X11ScreenCaptureTests.cs`. Its tests compile on Linux only together with `fix/tests-non-windows-build`.
- [ ] Process scans on every region hotkey press. `PortalBackendDetector.DetectX11RegionSupport(...)` takes `ProbeRunningBackends()` as an argument, so all 7 `Process.GetProcessesByName` scans run before the `hasScreenshotPortal` check returns. With 1261 processes: 8.6 ms per scan, 72 ms per `ProbeRunningBackends`, 59 ms per `LinuxRuntimeContextDetector.Detect`, 113 ms per `LinuxRegionSelectorDiagnosticsDetector.Detect`. A press makes 4 to 5 such detections: 257 to 317 ms before the `region capture started` milestone, 59 of the 78 to 107 ms pre-capture. Fix: scan only when a portal exists, read `/proc` once for all 7 names, reuse 1 detection per press. Probe: `/workspace/scratch/xerahs-captest/detectprobe/detect.cs`.
- [ ] `SettingsBaseBackupDiagnosticsTests.TryRestoreUnixFileMode` restores read and write without execute, so the month folder it locks stays `drw-rw-rw-` under `tests/XerahS.Tests/bin`, and nothing inside can be listed or deleted. `TearDown`'s recursive delete then throws `UnauthorizedAccessException` on `locked.zip`, which its catch swallows. Reproduced with the same `File.SetUnixFileMode` calls: 0666 on the folder leaves the delete failing, 0755 lets it succeed. Fix: restore the folder with the execute bits (0755) in `PruneOldBackups_PerFolderDeleteFailure_FiresSettingsBackupFailed_PruneFolderPhase`.
