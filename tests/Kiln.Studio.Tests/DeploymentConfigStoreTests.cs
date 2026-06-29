namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public class DeploymentConfigStoreTests
{
    [Test]
    public async Task SaveLoad_RoundTrip_PreservesConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var store = new DeploymentConfigStore();

        try
        {
            var original = new DeploymentConfig(
                DeploymentVariant.Filesystem,
                "/tmp/my-site-output",
                FilesystemMode.Zip);

            store.Save(tempDir, original);

            var loaded = store.Load(tempDir);

            await Assert.That(loaded.Variant).IsEqualTo(DeploymentVariant.Filesystem);
            await Assert.That(loaded.FilesystemPath).IsEqualTo("/tmp/my-site-output");
            await Assert.That(loaded.FilesystemMode).IsEqualTo(FilesystemMode.Zip);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Load_MissingFile_ReturnsNone()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var store = new DeploymentConfigStore();

        try
        {
            var loaded = store.Load(tempDir);

            await Assert.That(loaded.Variant).IsEqualTo(DeploymentVariant.None);
            await Assert.That(loaded.FilesystemPath).IsNull();
            await Assert.That(loaded.FilesystemMode).IsEqualTo(FilesystemMode.PlainCopy);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_CreatesDotKilnDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var store = new DeploymentConfigStore();

        try
        {
            var config = new DeploymentConfig(DeploymentVariant.GitHubPages, null, FilesystemMode.PlainCopy);
            store.Save(tempDir, config);

            await Assert.That(File.Exists(Path.Combine(tempDir, ".kiln", "deploy.json"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task SaveLoad_RoundTrip_GitHubPagesVariant()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var store = new DeploymentConfigStore();

        try
        {
            var original = new DeploymentConfig(DeploymentVariant.GitHubPages, null, FilesystemMode.PlainCopy);
            store.Save(tempDir, original);

            var loaded = store.Load(tempDir);

            await Assert.That(loaded.Variant).IsEqualTo(DeploymentVariant.GitHubPages);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
