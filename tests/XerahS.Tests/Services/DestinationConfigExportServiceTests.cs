#region License Information (GPL v3)

/*
    XerahS - The Avalonia UI implementation of ShareX
    Copyright (c) 2007-2026 ShareX Team
*/

#endregion License Information (GPL v3)

using NUnit.Framework;
using XerahS.Common;
using XerahS.Core;
using XerahS.UI.Services;
using XerahS.Uploaders.PluginSystem;

namespace XerahS.Tests.Services;

[TestFixture]
[NonParallelizable]
public class DestinationConfigExportServiceTests
{
    private string _rootPath = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _rootPath = Path.Combine(Path.GetTempPath(), "XerahS.Tests", "DestinationConfig", Guid.NewGuid().ToString("N"));
        var personalFolder = Path.Combine(_rootPath, "Personal");

        Directory.CreateDirectory(_rootPath);
        PathsManager.PersonalFolder = personalFolder;
        SettingsManager.PersonalFolder = personalFolder;
        PathsManager.EnsureDirectoriesExist();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        ClearInstances();

        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }

    [SetUp]
    public void SetUp()
    {
        ClearInstances();
        ProviderCatalog.SetProviderContext(new TestProviderContext(new InMemorySecretStore()));
    }

    [Test]
    public void BuildEncryptedExport_S3WithoutBucket_ThrowsBeforeExportingIncompleteMobileConfig()
    {
        var instance = new UploaderInstance
        {
            InstanceId = "s3-default",
            ProviderId = "amazons3",
            Category = UploaderCategory.File,
            DisplayName = "S3",
            SettingsJson = """
            {
              "AuthMode": 0,
              "SecretKey": "secret-key",
              "BucketName": "   ",
              "Region": "us-west-2"
            }
            """
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            DestinationConfigExportService.BuildEncryptedExport(instance, "correct horse battery staple"));

        Assert.That(ex!.Message, Is.EqualTo("Amazon S3 bucket name is required before exporting to mobile."));
    }

    private static void ClearInstances()
    {
        foreach (var instance in InstanceManager.Instance.GetInstances().ToList())
        {
            InstanceManager.Instance.RemoveInstance(instance.InstanceId);
        }
    }

    private sealed class TestProviderContext(ISecretStore secrets) : IProviderContext
    {
        public ISecretStore Secrets { get; } = secrets;
    }

    private sealed class InMemorySecretStore : ISecretStore
    {
        private readonly Dictionary<(string ProviderId, string SecretKey, string Name), string> _values = new();

        public string? GetSecret(string providerId, string secretKey, string name)
        {
            return _values.TryGetValue((providerId, secretKey, name), out var value) ? value : null;
        }

        public void SetSecret(string providerId, string secretKey, string name, string value)
        {
            _values[(providerId, secretKey, name)] = value;
        }

        public void DeleteSecret(string providerId, string secretKey, string name)
        {
            _values.Remove((providerId, secretKey, name));
        }

        public bool HasSecret(string providerId, string secretKey, string name)
        {
            return _values.ContainsKey((providerId, secretKey, name));
        }
    }
}
