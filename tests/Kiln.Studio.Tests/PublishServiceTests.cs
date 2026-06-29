namespace Kiln.Studio.Tests;

using System.IO.Compression;
using Kiln.Services;
using Kiln.Studio.Services;
using Microsoft.Extensions.DependencyInjection;

public class PublishServiceTests
{
    private static string CreateSite(string parentDir, string siteName)
    {
        var host = new EngineHost();
        using var provider = host.CreateProvider(parentDir);
        var scaffolder = provider.GetRequiredService<IScaffolder>();
        return scaffolder.CreateSite(siteName, parentDir).ProjectPath;
    }

    [Test]
    public async Task PublishAsync_PlainCopy_CopiesSiteFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var targetDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        try
        {
            var projectPath = CreateSite(tempDir, "publish-plain");
            var service = new PublishService(new EngineHost());

            var config = new DeploymentConfig(DeploymentVariant.Filesystem, targetDir, FilesystemMode.PlainCopy);
            var result = await service.PublishAsync(projectPath, config);

            await Assert.That(result.Success).IsTrue();
            await Assert.That(result.Destination).IsEqualTo(targetDir);
            await Assert.That(result.FileCount).IsGreaterThan(0);
            await Assert.That(File.Exists(Path.Combine(targetDir, "index.html"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, recursive: true);
        }
    }

    [Test]
    public async Task PublishAsync_Zip_CreatesZipFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var zipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");

        try
        {
            var projectPath = CreateSite(tempDir, "publish-zip");
            var service = new PublishService(new EngineHost());

            var config = new DeploymentConfig(DeploymentVariant.Filesystem, zipPath, FilesystemMode.Zip);
            var result = await service.PublishAsync(projectPath, config);

            await Assert.That(result.Success).IsTrue();
            await Assert.That(result.Destination).IsEqualTo(zipPath);
            await Assert.That(File.Exists(zipPath)).IsTrue();

#pragma warning disable S6966
            using var archive = ZipFile.OpenRead(zipPath);
#pragma warning restore S6966
            var entryNames = archive.Entries.Select(e => e.FullName).ToList();
            await Assert.That(entryNames).Contains("index.html");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
            if (File.Exists(zipPath))
                File.Delete(zipPath);
        }
    }

    [Test]
    public async Task PublishAsync_WrongVariant_ReturnsFailure()
    {
        var service = new PublishService(new EngineHost());
        var config = new DeploymentConfig(DeploymentVariant.None, null, FilesystemMode.PlainCopy);
        var result = await service.PublishAsync("/tmp/nonexistent", config);

        await Assert.That(result.Success).IsFalse();
        await Assert.That(result.Error).IsNotNull();
    }
}
