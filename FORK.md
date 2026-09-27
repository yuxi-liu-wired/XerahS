# yuxi-liu-wired/XerahS

Fork of [ShareX/XerahS](https://github.com/ShareX/XerahS) for one target machine: Lubuntu 25.10, X11, spectrwm (tiling WM, every window tiled via `quirk[.*] = NONE`), keyd remapping the keyboard, a single 2560x1600 panel at Avalonia scale 2.25.

Upstream base: `1f4249d` (`develop`, the upstream default branch). The released AppImage in use is v0.28.32.

## Remotes

- `origin`: <https://github.com/yuxi-liu-wired/XerahS>
- `upstream`: <https://github.com/ShareX/XerahS>

## Changes from upstream

- Linux only. The fork builds, tests, and ships Linux alone: `XerahS.Platform.Windows`, `XerahS.Platform.MacOS`, `XerahS.Platform.Mobile`, `src/mobile-experimental/`, `native/macos/`, their tests, `build/windows`, `build/macos`, `build/android`, the PowerShell build and hook scripts, and the chocolatey workflow are deleted. Every project targets `net10.0` with `LINUX` defined and references `XerahS.Platform.Linux` unconditionally. The `#if WINDOWS`/`MACOS`/`LINUX` blocks are resolved to their Linux branch. The release workflow and `build/ci/validate_release_assets.py` cover the Linux packages only. The PowerShell scripts under `.ai/skills/` and `docs/audits/` remain as upstream maintainer tooling and archived audit scripts. Upstream syncs will conflict wherever upstream edits the deleted files or the Windows branches of shared code.
- Linux only, shared code. The macOS region-selector and capture-sound settings, DPAPI settings encryption, the DPAPI and Keychain secret stores, `IScreenCapturePermissionService`, the Windows and macOS members of `IPlatformInfo` and `PlatformType`, the ffprobe fallback download (Windows binaries only), the Windows-only recorder frame dumps with `workflow run --dump-frame`, and the unused Windows System.Drawing helpers are removed. Every `OperatingSystem.IsWindows()`/`IsMacOS()` branch is folded to its Linux result, so Linux behavior is unchanged except for these: the GitHub Gist plugin now opens its login URL (it only did on Windows), and the startup log reports `Environment.IsPrivilegedProcess` instead of a hard-coded false. `GradientInfo`, `Extensions`, and `GraphicsExtensions` keep their `[SupportedOSPlatform("windows")]`: the attribute marks System.Drawing use, and the image beautifier still uses `GradientInfo`. `IsLinux()` guards stay where the platform analyzer needs them.
- Updates come from this fork. `UpdateService` resolves every update channel to this repository's releases (the pre-release source picker with its ShareX, KovaForge, custom, and any options is removed), and the release workflow publishes the fork's tagged releases as full releases, so the Release channel sees them. The checker takes the `-linux-x64.deb`/`.tar.gz`/`.rpm` asset of a `vX.Y.Z` release, or the `.tar.gz`/`.AppImage` for a portable install.
- `src/platform/XerahS.Platform.Linux/Capture/X11/X11ScreenCapture.cs`: the XGetImage-to-SKBitmap conversion wrote every pixel through `SKBitmap.SetPixel`, one native call per pixel, 4,096,000 calls at 2560x1600. The measured cost in the v0.28.32 log is 3.1 s to 7.3 s per capture. The new code builds each row in an `int[]` and copies it into `bitmap.GetPixels()` with one `Marshal.Copy` per row. For the common 32 bpp LSBFirst BGRX layout the source row is copied directly and the alpha byte set to 0xFF. Built into the 0.28.35 packages in `dist/`. Tested on Xvfb at 2560x1600: output byte-identical to the old code at depth 24 and 30, capture 13 ms instead of 4.1 s, region overlay invoked 78 to 109 ms after the hotkey. Not yet run on the target machine; see `TODO.md`. The upstream version of this change is branch `fix/x11-capture-row-copy`: the same conversion moved into `ConvertZPixmap`, with unit tests.

## Problems found on the target machine

Each one is an item in `TODO.md`.

- Region capture: 3 to 8 s from hotkey to overlay (the SetPixel loop above, run by the pre-capture).
- Global hotkeys: dead under keyd. The evdev backend reads `/dev/input/event3`, keyd holds an exclusive grab on it, and the enumerator skips the keyd virtual keyboard as virtual.
- Region overlay: tiled by spectrwm, so the frozen screenshot and the pointer disagree.
- Crash: `ObjectDisposedException` when another app pastes a clipboard image after XerahS disposed the bitmap.
