namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public class AssetLibraryServiceTests
{
    private const int TwoEntries = 2;

    private static string CreateTempProject()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "static"));
        return dir;
    }

    [Test]
    public async Task ListFolder_Root_ReturnsFoldersBeforeFiles()
    {
        var projectPath = CreateTempProject();
        try
        {
            var staticDir = Path.Combine(projectPath, "static");
            Directory.CreateDirectory(Path.Combine(staticDir, "images"));
            await File.WriteAllTextAsync(Path.Combine(staticDir, "readme.txt"), "hello");

            var service = new AssetLibraryService();
            var entries = service.ListFolder(projectPath, "");

            await Assert.That(entries.Count).IsEqualTo(TwoEntries);
            await Assert.That(entries[0].Name).IsEqualTo("images");
            await Assert.That(entries[0].IsFolder).IsTrue();
            await Assert.That(entries[0].RelativePath).IsEqualTo("images");
            await Assert.That(entries[1].Name).IsEqualTo("readme.txt");
            await Assert.That(entries[1].IsFolder).IsFalse();
            await Assert.That(entries[1].RelativePath).IsEqualTo("readme.txt");
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task ListFolder_MissingFolder_ReturnsEmpty()
    {
        var projectPath = CreateTempProject();
        try
        {
            var service = new AssetLibraryService();
            var entries = service.ListFolder(projectPath, "does-not-exist");

            await Assert.That(entries.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task ListFolder_Subfolder_ReturnsRelativePathWithParent()
    {
        var projectPath = CreateTempProject();
        try
        {
            Directory.CreateDirectory(Path.Combine(projectPath, "static", "images"));
            await File.WriteAllTextAsync(Path.Combine(projectPath, "static", "images", "photo.png"), "fake-png");

            var service = new AssetLibraryService();
            var entries = service.ListFolder(projectPath, "images");

            await Assert.That(entries.Count).IsEqualTo(1);
            await Assert.That(entries[0].RelativePath).IsEqualTo("images/photo.png");
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task CreateFolder_CreatesDirectoryUnderStatic()
    {
        var projectPath = CreateTempProject();
        try
        {
            var service = new AssetLibraryService();
            service.CreateFolder(projectPath, "", "downloads");

            await Assert.That(Directory.Exists(Path.Combine(projectPath, "static", "downloads"))).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task Upload_CopiesFileAndReturnsRelativePath()
    {
        var projectPath = CreateTempProject();
        var sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDir);
        try
        {
            var sourceFile = Path.Combine(sourceDir, "photo.png");
            await File.WriteAllTextAsync(sourceFile, "fake-png");

            var service = new AssetLibraryService();
            var relativePath = service.Upload(projectPath, "images", sourceFile);

            await Assert.That(relativePath).IsEqualTo("images/photo.png");
            await Assert.That(File.Exists(Path.Combine(projectPath, "static", "images", "photo.png"))).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Test]
    public async Task Upload_NameCollision_ResolvesWithNumericSuffix()
    {
        var projectPath = CreateTempProject();
        var sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDir);
        try
        {
            var sourceFile = Path.Combine(sourceDir, "photo.png");
            await File.WriteAllTextAsync(sourceFile, "fake-png");

            var service = new AssetLibraryService();
            var first = service.Upload(projectPath, "", sourceFile);
            var second = service.Upload(projectPath, "", sourceFile);

            await Assert.That(first).IsEqualTo("photo.png");
            await Assert.That(second).IsEqualTo("photo-2.png");
            await Assert.That(File.Exists(Path.Combine(projectPath, "static", "photo.png"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(projectPath, "static", "photo-2.png"))).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }
}
