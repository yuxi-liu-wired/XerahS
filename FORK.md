# yuxi-liu-wired/XerahS

Fork of [ShareX/XerahS](https://github.com/ShareX/XerahS) for one target machine: Lubuntu 25.10, X11, spectrwm (tiling WM, every window tiled via `quirk[.*] = NONE`), keyd remapping the keyboard, a single 2560x1600 panel at Avalonia scale 2.25.

Upstream base: `1f4249d` (main as cloned). The released AppImage in use is v0.28.32.

## Remotes

- `origin`: <https://github.com/yuxi-liu-wired/XerahS>
- `upstream`: <https://github.com/ShareX/XerahS>

## Changes from upstream

- `src/platform/XerahS.Platform.Linux/Capture/X11/X11ScreenCapture.cs`: the XGetImage-to-SKBitmap conversion wrote every pixel through `SKBitmap.SetPixel`, one native call per pixel, 4,096,000 calls at 2560x1600. The measured cost in the v0.28.32 log is 3.1 s to 7.3 s per capture. The new code builds each row in an `int[]` and copies it into `bitmap.GetPixels()` with one `Marshal.Copy` per row. For the common 32 bpp LSBFirst BGRX layout the source row is copied directly and the alpha byte set to 0xFF. This commit is unbuilt and untested; see `TODO.md`.

## Problems found on the target machine

Each one is an item in `TODO.md`.

- Region capture: 3 to 8 s from hotkey to overlay (the SetPixel loop above, run by the pre-capture).
- Global hotkeys: dead under keyd. The evdev backend reads `/dev/input/event3`, keyd holds an exclusive grab on it, and the enumerator skips the keyd virtual keyboard as virtual.
- Region overlay: tiled by spectrwm, so the frozen screenshot and the pointer disagree.
- Crash: `ObjectDisposedException` when another app pastes a clipboard image after XerahS disposed the bitmap.
