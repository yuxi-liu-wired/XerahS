#!/usr/bin/env python3
"""Archive fixtures for Linux release validation; no release downloads required."""

import tarfile
import tempfile
import unittest
from io import BytesIO
from pathlib import Path

from validate_release_assets import (
    EXPECTED_ASSETS,
    build_file_name,
    ensure_tar_has_daemon,
)


class LinuxReleaseTests(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp_dir.cleanup)
        self.path = Path(self.temp_dir.name) / "linux.tar.gz"
        self.members = {
            "./XerahS": b"app",
            "./xerahs-watchfolder-daemon": b"daemon",
            "./xerahs-watchfolder-daemon.runtimeconfig.json": b"{}",
            "./omaxerahs": b"cli",
            "./omaxerahs.runtimeconfig.json": b"{}",
        }

    def write_archive(self):
        with tarfile.open(self.path, "w:gz") as archive:
            for name, content in self.members.items():
                info = tarfile.TarInfo(name)
                info.size = len(content)
                archive.addfile(info, BytesIO(content))

    def test_every_linux_package_is_required_for_both_architectures(self):
        for arch in ("x64", "arm64"):
            for extension in ("tar.gz", "deb", "rpm", "AppImage"):
                self.assertIn(("linux", arch, extension), EXPECTED_ASSETS)
        self.assertEqual({os_name for os_name, _, _ in EXPECTED_ASSETS}, {"linux"})
        self.assertEqual(
            build_file_name("1.2.3", "linux", "arm64", "AppImage"),
            "XerahS-1.2.3-linux-arm64.AppImage",
        )

    def test_complete_archive_passes(self):
        self.write_archive()
        ensure_tar_has_daemon(self.path, "linux")

    def test_each_bundled_executable_and_runtimeconfig_is_required(self):
        for name in list(self.members):
            if name == "./XerahS":
                continue
            with self.subTest(name=name):
                content = self.members.pop(name)
                self.write_archive()
                with self.assertRaisesRegex(RuntimeError, "Missing"):
                    ensure_tar_has_daemon(self.path, "linux")
                self.members[name] = content


if __name__ == "__main__":
    unittest.main()
