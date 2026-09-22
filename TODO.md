# TODO

Target machine: Lubuntu 25.10, X11, spectrwm, keyd, 2560x1600 panel, Avalonia scale 2.25. Log file on the target: `~/.local/state/xerahs/Logs/2026-09/XerahS-20260921.log`. Current deployment: `~/Applications/XerahS-0.28.32-linux-x64.AppImage` run by `~/.config/systemd/user/xerahs.service` (Restart=always, `XERAHS_LINUX_HOTKEY_BACKEND=x11`).

## 1. Build and verify the X11 capture fix

- [ ] Get a .NET 10 SDK (conda-forge `dotnet` 10.0.401 via pixi works; no system install).
- [ ] Build `src/desktop/app/XerahS.App` for `linux-x64`, Release.
- [ ] Confirm `X11ScreenCapture.CaptureFullScreen` output matches the old code pixel for pixel (compare against a `maim` capture of a static screen).
- [ ] Measure: log milestone `region capture UI invoked (+N ms)` for the RectangleRegion job. Old value 3111 to 7321 ms.
- [ ] Point `xerahs.service` `ExecStart` at the built binary.

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

- [ ] Produce a linux-x64 AppImage or self-contained publish folder from the fork.
- [ ] Rebase on `upstream/main` when upstream moves.
