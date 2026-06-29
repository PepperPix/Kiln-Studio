namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public class SiteSettingsServiceTests
{
    [Test]
    public async Task Load_ScaffoldedSite_ReturnsExpectedValues()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var projectPath = new ProjectService(new EngineHost()).CreateSite(tempDir, "TestSite");
            var sut = new SiteSettingsService(new EngineHost());

            var settings = sut.Load(projectPath);

            await Assert.That(settings.Title).IsEqualTo("TestSite");
            await Assert.That(settings.Language).IsEqualTo("en");
            await Assert.That(settings.Theme).IsEqualTo("default");
            await Assert.That(settings.BaseUrl).Contains("localhost");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task ListThemes_ScaffoldedSite_ContainsDefault()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var projectPath = new ProjectService(new EngineHost()).CreateSite(tempDir, "ThemeSite");
            var sut = new SiteSettingsService(new EngineHost());

            var themes = sut.ListThemes(projectPath);

            await Assert.That(themes.Contains("default")).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_ChangedTitle_UpdatesTitlePreservesCollectionsAndMenus()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
#pragma warning disable S3353
            var siteYaml =
                """
                title: Original Title
                description: A test site
                baseUrl: http://localhost:5555
                language: en
                theme: default

                # This comment must survive
                collections:
                  posts:
                    directory: content/posts
                    permalink: /blog/:slug/

                menus:
                  main:
                    - title: Home
                      url: /

                taxonomies:
                  tags:
                    permalink: /tags/:slug/
                """;
#pragma warning restore S3353
            var projectPath = Path.Combine(tempDir, "mysite");
            Directory.CreateDirectory(projectPath);
            await File.WriteAllTextAsync(Path.Combine(projectPath, "site.yaml"), siteYaml);

            var sut = new SiteSettingsService(new EngineHost());
            var original = sut.Load(projectPath);
            var updated = original with { Title = "Updated Title" };

            sut.Save(projectPath, updated);

            var resultText = await File.ReadAllTextAsync(Path.Combine(projectPath, "site.yaml"));
            await Assert.That(resultText).Contains("title: Updated Title");
            await Assert.That(resultText).Contains("# This comment must survive");
            await Assert.That(resultText).Contains("collections:");
            await Assert.That(resultText).Contains("  posts:");
            await Assert.That(resultText).Contains("menus:");
            await Assert.That(resultText).Contains("taxonomies:");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_MissingKey_AppendsKeyAtTop()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
#pragma warning disable S3353
            var siteYaml =
                """
                title: My Site
                baseUrl: http://localhost:5555
                language: en
                theme: default

                home:
                  page: content/index.md

                collections:
                  posts:
                    directory: content/posts
                """;
#pragma warning restore S3353
            var projectPath = Path.Combine(tempDir, "nodescrip");
            Directory.CreateDirectory(projectPath);
            await File.WriteAllTextAsync(Path.Combine(projectPath, "site.yaml"), siteYaml);

            var sut = new SiteSettingsService(new EngineHost());
            var loaded = sut.Load(projectPath);
            var settings = loaded with { Description = "Now added" };

            sut.Save(projectPath, settings);

            var result = await File.ReadAllTextAsync(Path.Combine(projectPath, "site.yaml"));
            await Assert.That(result).Contains("description: Now added");
            await Assert.That(result).Contains("collections:");
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task ReadRawYaml_WriteRawYaml_RoundTrip()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var projectPath = Path.Combine(tempDir, "rawsite");
            Directory.CreateDirectory(projectPath);
            const string original = "title: RawTest\nbaseUrl: http://localhost\n";
            await File.WriteAllTextAsync(Path.Combine(projectPath, "site.yaml"), original);

            var sut = new SiteSettingsService(new EngineHost());

            var read = sut.ReadRawYaml(projectPath);
            sut.WriteRawYaml(projectPath, read);
            var readAgain = sut.ReadRawYaml(projectPath);

            await Assert.That(readAgain).IsEqualTo(original);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Save_RestOfFile_NonTitleLinesArePreserved()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
#pragma warning disable S3353
            // Use trailing slash on baseUrl so Load→Save does not normalize it
            var siteYaml =
                """
                title: Preserve Test
                description: keep this
                baseUrl: http://localhost:5555/
                language: en
                theme: default

                home:
                  page: content/index.md

                collections:
                  posts:
                    directory: content/posts
                    permalink: /blog/:slug/
                """;
#pragma warning restore S3353
            var projectPath = Path.Combine(tempDir, "preserve");
            Directory.CreateDirectory(projectPath);
            await File.WriteAllTextAsync(Path.Combine(projectPath, "site.yaml"), siteYaml);

            var sut = new SiteSettingsService(new EngineHost());
            var loaded = sut.Load(projectPath);
            var settings = loaded with { Title = "Changed Title" };

            sut.Save(projectPath, settings);

            var result = await File.ReadAllTextAsync(Path.Combine(projectPath, "site.yaml"));

            await Assert.That(result).Contains("title: Changed Title");

            var resultNonTitle = string.Join('\n', result.Split('\n')
                .Where(l => !l.StartsWith("title:", StringComparison.Ordinal)));
            var originalNonTitle = string.Join('\n', siteYaml.Split('\n')
                .Where(l => !l.StartsWith("title:", StringComparison.Ordinal)));
            await Assert.That(resultNonTitle).IsEqualTo(originalNonTitle);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
