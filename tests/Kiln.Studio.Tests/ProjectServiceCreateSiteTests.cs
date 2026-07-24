namespace Kiln.Studio.Tests;

using Services;

public class ProjectServiceCreateSiteTests
{
    private const string SiteName = "my-new-site";

    [Test]
    public async Task CreateSite_ValidArgs_ReturnsExistingPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ProjectService(new EngineHost());
            var projectPath = service.CreateSite(tempDir, SiteName);

            await Assert.That(Directory.Exists(projectPath)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSite_ThenOpen_HasCollections()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ProjectService(new EngineHost());
            var projectPath = service.CreateSite(tempDir, SiteName);
            var opened = service.Open(projectPath);

            await Assert.That(opened.Collections.Count).IsGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSite_EmptySiteName_ThrowsArgumentException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ProjectService(new EngineHost());

            await Assert.That(() => service.CreateSite(tempDir, ""))
                .Throws<ArgumentException>();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSite_NonExistentParent_ThrowsArgumentException()
    {
        var service = new ProjectService(new EngineHost());

        await Assert.That(() => service.CreateSite("/no/such/directory", SiteName))
            .Throws<ArgumentException>();
    }
}
