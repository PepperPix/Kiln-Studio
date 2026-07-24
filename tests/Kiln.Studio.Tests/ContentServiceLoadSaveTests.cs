namespace Kiln.Studio.Tests;

using Models;
using Kiln.Services;
using Services;
using Microsoft.Extensions.DependencyInjection;

public class ContentServiceLoadSaveTests
{
    private const string PostsCollection = "posts";
    private const string PostsDirectory = "content/posts";

    [Test]
    public async Task Load_SplitsFrontmatterAndBody()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                "---\ntitle: Hello World\ndate: 2026-06-25\n---\n\nMarkdown body here.");

            var service = new ContentService();
            var doc = service.Load(filePath);

            await Assert.That(doc.FrontMatter).Contains("title: Hello World");
            await Assert.That(doc.Body).Contains("Markdown body here.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Load_NoFrontmatter_ReturnsEmptyFrontmatterAndFullBody()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, "Just body content.");

            var service = new ContentService();
            var doc = service.Load(filePath);

            await Assert.That(doc.FrontMatter).IsEqualTo("");
            await Assert.That(doc.Body).Contains("Just body content.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task RoundTrip_LoadThenSave_EngineCanStillReadTitle()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempDir);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("round-trip-site", tempDir);
            var projectPath = scaffoldResult.ProjectPath;

            var postsDir = Path.Combine(projectPath, PostsDirectory);
            var mdFile = Directory.GetFiles(postsDir, "*.md", SearchOption.TopDirectoryOnly)
                .First();

            var service = new ContentService();
            var doc = service.Load(mdFile);
            var originalTitle = doc.FrontMatter;

            service.Save(mdFile, doc.FrontMatter, doc.Body);

            using var readProvider = host.CreateProvider(projectPath);
            var siteConfig = readProvider.GetRequiredService<ISiteConfigLoader>().Load(projectPath);
            var postsGroup = siteConfig.Collections[PostsCollection];
            var reader = readProvider.GetRequiredService<IContentReader>();
            var item = reader.ReadSingleFile(mdFile, postsGroup);

            await Assert.That(item.Title).IsNotNull();
            await Assert.That(item.Title).IsNotEqualTo("");
            await Assert.That(doc.FrontMatter).IsEqualTo(originalTitle);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
