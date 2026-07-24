namespace Kiln.Studio.Tests;

using Kiln.Services;
using Services;
using Services.Dto;
using Microsoft.Extensions.DependencyInjection;

public class BuildServiceTests
{
    [Test]
    public async Task BuildAsync_DebugBuild_SucceedsAndCreatesIndexHtml()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectPath = CreateSite(tempDir, "build-debug");
            var service = new BuildService(new EngineHost());

            var result = await service.BuildAsync(projectPath, release: false);

            await Assert.That(result.Success).IsTrue();
            await Assert.That(result.RenderedFiles).IsGreaterThan(0);
            await Assert.That(Directory.Exists(result.OutputDirectory)).IsTrue();
            await Assert.That(File.Exists(Path.Combine(result.OutputDirectory, "index.html"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task BuildAsync_ReleaseBuild_SucceedsAndCreatesIndexHtml()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectPath = CreateSite(tempDir, "build-release");
            var service = new BuildService(new EngineHost());

            var result = await service.BuildAsync(projectPath, release: true);

            await Assert.That(result.Success).IsTrue();
            await Assert.That(result.RenderedFiles).IsGreaterThan(0);
            await Assert.That(Directory.Exists(result.OutputDirectory)).IsTrue();
            await Assert.That(File.Exists(Path.Combine(result.OutputDirectory, "index.html"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateSite(string parentDir, string siteName)
    {
        var host = new EngineHost();
        using var provider = host.CreateProvider(parentDir);
        var scaffolder = provider.GetRequiredService<IScaffolder>();
        return scaffolder.CreateSite(siteName, parentDir).ProjectPath;
    }
}
