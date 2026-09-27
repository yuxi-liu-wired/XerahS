#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using System.Runtime.InteropServices;
using NUnit.Framework;
using XerahS.Common;

namespace XerahS.Tests.Helpers;

[TestFixture]
public class GitHubUpdateCheckerAssetSelectionTests
{
    private static readonly string[] AllReleaseAssets =
    [
        "XerahS-1.2.3-win-x64.exe",
        "XerahS-1.2.3-win-arm64.exe",
        "XerahS-1.2.3-mac-x64.tar.gz",
        "XerahS-1.2.3-mac-arm64.tar.gz",
        "XerahS-1.2.3-linux-x64.tar.gz",
        "XerahS-1.2.3-linux-x64.deb",
        "XerahS-1.2.3-linux-x64.rpm",
        "XerahS-1.2.3-linux-x64.AppImage",
        "XerahS-1.2.3-linux-arm64.tar.gz",
        "XerahS-1.2.3-linux-arm64.deb",
        "XerahS-1.2.3-linux-arm64.rpm",
        "XerahS-1.2.3-linux-arm64.AppImage"
    ];

    [TestCase(Architecture.X64, "XerahS-1.2.3-linux-x64.deb")]
    [TestCase(Architecture.Arm64, "XerahS-1.2.3-linux-arm64.deb")]
    public void UpdateReleaseInfo_SelectsAssetForEachArchitecture(Architecture architecture, string expectedAsset)
    {
        var checker = new TestGitHubUpdateChecker("linux", architecture);

        bool resolved = checker.TryResolveAssets(AllReleaseAssets, isPortable: false);

        Assert.That(resolved, Is.True);
        Assert.That(checker.FileName, Is.EqualTo(expectedAsset));
    }

    [Test]
    public void UpdateReleaseInfo_OnLinux_DoesNotFallbackToOtherPlatformAssets()
    {
        var checker = new TestGitHubUpdateChecker("linux", Architecture.X64);
        string[] otherPlatformAssets =
        [
            "XerahS-1.2.3-win-x64.exe",
            "XerahS-setup.exe",
            "XerahS-1.2.3-mac-x64.tar.gz"
        ];

        bool resolved = checker.TryResolveAssets(otherPlatformAssets, isPortable: false);

        Assert.That(resolved, Is.False);
        Assert.That(checker.DownloadURL, Is.Null);
    }

    [Test]
    public void UpdateReleaseInfo_OnLinuxPortable_PrefersTarball()
    {
        var checker = new TestGitHubUpdateChecker("linux", Architecture.Arm64);
        string[] assets =
        [
            "XerahS-1.2.3-linux-arm64.deb",
            "XerahS-1.2.3-linux-arm64.tar.gz"
        ];

        bool resolved = checker.TryResolveAssets(assets, isPortable: true);

        Assert.That(resolved, Is.True);
        Assert.That(checker.FileName, Is.EqualTo("XerahS-1.2.3-linux-arm64.tar.gz"));
    }

    [Test]
    public void UpdateReleaseInfo_OnLinuxPortable_FallsBackToAppImageWhenTarballMissing()
    {
        var checker = new TestGitHubUpdateChecker("linux", Architecture.X64);
        string[] assets =
        [
            "XerahS-1.2.3-linux-x64.deb",
            "XerahS-1.2.3-linux-x64.AppImage"
        ];

        bool resolved = checker.TryResolveAssets(assets, isPortable: true);

        Assert.That(resolved, Is.True);
        Assert.That(checker.FileName, Is.EqualTo("XerahS-1.2.3-linux-x64.AppImage"));
    }

    private sealed class TestGitHubUpdateChecker(string runtimePlatform, Architecture architecture) : GitHubUpdateChecker("ShareX", "XerahS")
    {
        private readonly string _runtimePlatform = runtimePlatform;
        private readonly Architecture _architecture = architecture;

        protected override Architecture GetProcessArchitecture()
        {
            return _architecture;
        }

        protected override RuntimePlatform GetRuntimePlatform()
        {
            return _runtimePlatform.ToLowerInvariant() switch
            {
                "linux" => RuntimePlatform.Linux,
                _ => RuntimePlatform.Unknown
            };
        }

        public bool TryResolveAssets(string[] assetNames, bool isPortable)
        {
            GitHubAsset[] assets = new GitHubAsset[assetNames.Length];

            for (int i = 0; i < assetNames.Length; i++)
            {
                string assetName = assetNames[i];
                assets[i] = new GitHubAsset
                {
                    name = assetName,
                    browser_download_url = $"https://example.invalid/{assetName}",
                    url = $"https://api.example.invalid/{assetName}"
                };
            }

            GitHubRelease release = new GitHubRelease
            {
                tag_name = "v1.2.3",
                assets = assets
            };

            return UpdateReleaseInfo(release, isPortable, isBrowserDownloadURL: true);
        }
    }
}
