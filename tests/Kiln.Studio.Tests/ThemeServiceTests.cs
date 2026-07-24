namespace Kiln.Studio.Tests;

using System.IO.Compression;
using Services;

public class ThemeServiceTests
{
    private const int ExpectedThemeCount = 2;

    [Test]
    public async Task ListThemes_ReturnsThemeDirectories()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            var themes = service.ListThemes(path);

            await Assert.That(themes.Count).IsEqualTo(ExpectedThemeCount);
            await Assert.That(themes).Contains("default");
            await Assert.That(themes).Contains("custom");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task GetCurrentTheme_ReturnsThemeFromSiteYaml()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            var theme = service.GetCurrentTheme(path);

            await Assert.That(theme).IsEqualTo("custom");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task SetCurrentTheme_UpdatesSiteYaml()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            service.SetCurrentTheme(path, "default");

            await Assert.That(service.GetCurrentTheme(path)).IsEqualTo("default");
            var yaml = await File.ReadAllTextAsync(Path.Combine(path, "site.yaml"));
            await Assert.That(yaml).Contains("theme: default");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task DuplicateTheme_CopiesThemeDirectory()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            service.DuplicateTheme(path, "default", "default-copy");

            await Assert.That(Directory.Exists(Path.Combine(path, "themes", "default-copy", "layouts"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(path, "themes", "default-copy", "layouts", "default.html"))).IsTrue();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task DuplicateTheme_TargetExists_Throws()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            await Assert.That(() => service.DuplicateTheme(path, "default", "custom"))
                .Throws<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task InstallThemeFromZip_ExtractsTheme()
    {
        var path = CreateSiteWithThemes();
        var (zipPath, baseName) = CreateThemeZip();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            var installedName = service.InstallThemeFromZip(path, zipPath);

            await Assert.That(installedName).IsEqualTo(baseName);
            await Assert.That(Directory.Exists(Path.Combine(path, "themes", baseName, "layouts"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(path, "themes", baseName, "layouts", "page.html"))).IsTrue();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
            File.Delete(zipPath);
        }
    }

    [Test]
    public async Task InstallThemeFromZip_ThemeExists_AppendsSuffix()
    {
        var path = CreateSiteWithThemes();
        var (zipPath, baseName) = CreateThemeZip();
        try
        {
            Directory.CreateDirectory(Path.Combine(path, "themes", baseName));
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            var installedName = service.InstallThemeFromZip(path, zipPath);

            await Assert.That(installedName).IsEqualTo($"{baseName}-1");
            await Assert.That(Directory.Exists(Path.Combine(path, "themes", $"{baseName}-1", "layouts"))).IsTrue();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
            File.Delete(zipPath);
        }
    }

    [Test]
    public async Task InstallThemeFromZip_WithoutLayouts_Throws()
    {
        var path = CreateSiteWithThemes();
        var zipPath = CreateInvalidZip();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            await Assert.That(() => service.InstallThemeFromZip(path, zipPath))
                .Throws<InvalidDataException>();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
            File.Delete(zipPath);
        }
    }

    [Test]
    public async Task ListThemeFiles_ReturnsEntriesWithKinds()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            var entries = service.ListThemeFiles(path, "default");

            var layoutEntry = entries.First(e => e.RelativePath == "layouts/default.html");
            var staticEntry = entries.First(e => e.RelativePath == "static/style.css");

            await Assert.That(layoutEntry.IsDirectory).IsFalse();
            await Assert.That(layoutEntry.Kind).IsEqualTo(ThemeFileKind.Layout);
            await Assert.That(staticEntry.Kind).IsEqualTo(ThemeFileKind.Static);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task ReadThemeFile_ReturnsContent()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var service = new ThemeService(new SiteSettingsService(new EngineHost()));

            var content = service.ReadThemeFile(path, "default", "layouts/default.html");

            await Assert.That(content).Contains("<html>");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static string CreateSiteWithThemes()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(path, "themes", "default", "layouts"));
        Directory.CreateDirectory(Path.Combine(path, "themes", "default", "static"));
        Directory.CreateDirectory(Path.Combine(path, "themes", "custom", "layouts"));

        File.WriteAllText(Path.Combine(path, "site.yaml"), """
            title: Test
            baseUrl: https://example.com
            theme: custom
            """);

        File.WriteAllText(Path.Combine(path, "themes", "default", "layouts", "default.html"), "<html></html>");
        File.WriteAllText(Path.Combine(path, "themes", "default", "static", "style.css"), "body {}");
        File.WriteAllText(Path.Combine(path, "themes", "custom", "layouts", "default.html"), "<html></html>");

        return path;
    }

    private static (string ZipPath, string BaseName) CreateThemeZip()
    {
        var baseName = Path.GetRandomFileName();
        var zipPath = Path.Combine(Path.GetTempPath(), baseName + ".zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        var entry = archive.CreateEntry("layouts/page.html");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write("<html></html>");
        return (zipPath, baseName);
    }

    private static string CreateInvalidZip()
    {
        var zipPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        archive.CreateEntry("readme.txt");
        return zipPath;
    }
}
