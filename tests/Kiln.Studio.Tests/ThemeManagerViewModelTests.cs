namespace Kiln.Studio.Tests;

using Services;
using TestSupport;
using ViewModels;

public class ThemeManagerViewModelTests
{
    [Test]
    public async Task LoadProject_PopulatesThemesAndFiles()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var vm = MakeVm(path);

            await Assert.That(vm.AvailableThemes).Contains("default");
            await Assert.That(vm.SelectedTheme).IsEqualTo("custom");
            await Assert.That(vm.ThemeFiles.Count).IsGreaterThan(0);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task ApplyThemeCommand_UpdatesCurrentTheme()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var vm = MakeVm(path);
            vm.SelectedTheme = "default";

            vm.ApplyThemeCommand.Execute(null);

            await Assert.That(vm.StatusMessage).Contains("Theme set to 'default'");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task DuplicateThemeCommand_ValidName_DuplicatesTheme()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var vm = new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new FixedInputDialog("default-copy"));
            vm.LoadProject(path);
            vm.SelectedTheme = "default";

            await vm.DuplicateThemeCommand.ExecuteAsync(null);

            await Assert.That(vm.AvailableThemes).Contains("default-copy");
            await Assert.That(vm.SelectedTheme).IsEqualTo("default-copy");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task DuplicateThemeCommand_InvalidName_ShowsStatusMessage()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var vm = new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new FixedInputDialog("theme/name"));
            vm.LoadProject(path);
            vm.SelectedTheme = "default";

            await vm.DuplicateThemeCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).Contains("invalid characters");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task DuplicateThemeCommand_DuplicateName_ShowsStatusMessage()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var vm = new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new FixedInputDialog("custom"));
            vm.LoadProject(path);
            vm.SelectedTheme = "default";

            await vm.DuplicateThemeCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).Contains("already exists");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task InstallFromZipCommand_InstallsTheme()
    {
        var path = CreateSiteWithThemes();
        var (zipPath, baseName) = CreateThemeZip();
        try
        {
            var vm = new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new FixedFilePicker(zipPath),
                new NullInputDialog());
            vm.LoadProject(path);

            await vm.InstallFromZipCommand.ExecuteAsync(null);

            await Assert.That(vm.AvailableThemes).Contains(baseName);
            await Assert.That(vm.SelectedTheme).IsEqualTo(baseName);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
            File.Delete(zipPath);
        }
    }

    [Test]
    public async Task SelectedFile_TextFile_LoadsContent()
    {
        var path = CreateSiteWithThemes();
        try
        {
            var vm = MakeVm(path);
            var file = vm.ThemeFiles.First(f => f.RelativePath == "layouts/default.html");
            vm.SelectedFile = file;

            await Assert.That(vm.SelectedFileContent).Contains("<html>");
            await Assert.That(vm.IsImageSelected).IsFalse();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static ThemeManagerViewModel MakeVm(string projectPath)
    {
        var vm = new ThemeManagerViewModel(
            new ThemeService(new SiteSettingsService(new EngineHost())),
            new NullFilePicker(),
            new NullInputDialog());
        vm.LoadProject(projectPath);
        return vm;
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
        using var archive = System.IO.Compression.ZipFile.Open(zipPath, System.IO.Compression.ZipArchiveMode.Create);
        var entry = archive.CreateEntry("layouts/page.html");
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write("<html></html>");
        return (zipPath, baseName);
    }
}
