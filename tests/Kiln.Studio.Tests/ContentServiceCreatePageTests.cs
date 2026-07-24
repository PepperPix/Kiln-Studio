namespace Kiln.Studio.Tests;

using Kiln.Services;
using Services;
using Microsoft.Extensions.DependencyInjection;

public class ContentServiceCreatePageTests
{
    private const string PageTitle = "My First Post";
    private const string ExpectedSlug = "my-first-post";
    private const string DuplicateSlug = "my-first-post-2";

    [Test]
    public async Task CreatePage_CreatesFileWithCorrectSlug()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            var path = service.CreatePage(tempDir, PageTitle);

            await Assert.That(Path.GetFileName(path)).IsEqualTo($"{ExpectedSlug}.md");
            await Assert.That(File.Exists(path)).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_FileContainsTitleAndDraftTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            var path = service.CreatePage(tempDir, PageTitle);
            var content = await File.ReadAllTextAsync(path);

            await Assert.That(content).Contains($"title: {PageTitle}");
            await Assert.That(content).Contains("draft: true");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_Collision_AddsSuffix()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            service.CreatePage(tempDir, PageTitle);
            var second = service.CreatePage(tempDir, PageTitle);

            await Assert.That(Path.GetFileName(second)).IsEqualTo($"{DuplicateSlug}.md");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_EmptyTitle_ThrowsArgumentException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            await Assert.That(() => service.CreatePage(tempDir, "")).Throws<ArgumentException>();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_CreatedFileReadableByEngine()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempDir);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("new-page-site", tempDir);
            var projectPath = scaffoldResult.ProjectPath;
            var postsDir = Path.Combine(projectPath, "content/posts");

            var service = new ContentService();
            var newFilePath = service.CreatePage(postsDir, PageTitle);

            using var readProvider = host.CreateProvider(projectPath);
            var siteConfig = readProvider.GetRequiredService<ISiteConfigLoader>().Load(projectPath);
            var postsGroup = siteConfig.Collections["posts"];
            var reader = readProvider.GetRequiredService<IContentReader>();
            var item = reader.ReadSingleFile(newFilePath, postsGroup);

            await Assert.That(item.Title).IsEqualTo(PageTitle);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
