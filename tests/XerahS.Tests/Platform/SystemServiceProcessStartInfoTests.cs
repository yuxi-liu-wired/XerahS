using System.IO;
using NUnit.Framework;
using XerahS.Platform.Linux.Services;

namespace XerahS.Tests.Platform;

public class SystemServiceProcessStartInfoTests
{
    [Test]
    public void LinuxSystemService_CreateOpenStartInfo_PreservesPathWithQuotesAndHashes_AsSingleArgument()
    {
        const string target = "/tmp/Capture \"draft\"/#1 shot.png";

        var startInfo = LinuxSystemService.CreateOpenStartInfo(target);

        Assert.Multiple(() =>
        {
            Assert.That(startInfo.FileName, Is.EqualTo("xdg-open"));
            Assert.That(startInfo.ArgumentList, Is.EqualTo(new[] { target }));
            Assert.That(startInfo.UseShellExecute, Is.False);
        });
    }

    [Test]
    public void LinuxSystemService_NormalizeExistingPath_ConvertsRelativeDashedPathToAbsolutePath()
    {
        string relativePath = Path.Combine(".", "-capture shot.png");

        string normalizedPath = LinuxSystemService.NormalizeExistingPath(relativePath);

        Assert.That(normalizedPath, Is.EqualTo(Path.GetFullPath(relativePath)));
        Assert.That(Path.IsPathRooted(normalizedPath), Is.True);
    }
}
