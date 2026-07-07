namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;

public class SettingsViewModelTests
{
    private static readonly string ProjectPath = Path.Combine(Path.GetTempPath(), "settings-vm-test");

    [Test]
    public async Task Load_FillsBasicFields()
    {
        var fake = new FakeSiteSettingsService
        {
            CurrentSettings = new("My Site", "Desc", "http://example.com/", "de", "dark")
        };
        var vm = new SettingsViewModel(fake, new NullDeploymentConfigStore());

        vm.Load(ProjectPath);

        await Assert.That(vm.Title).IsEqualTo("My Site");
        await Assert.That(vm.Description).IsEqualTo("Desc");
        await Assert.That(vm.BaseUrl).IsEqualTo("http://example.com/");
        await Assert.That(vm.Language).IsEqualTo("de");
        await Assert.That(vm.SelectedTheme).IsEqualTo("dark");
    }

    [Test]
    public async Task Load_FillsAvailableThemesAndSelectsCurrent()
    {
        const int expectedThemeCount = 3;
        var fake = new FakeSiteSettingsService
        {
            CurrentSettings = new("Site", "", "http://localhost/", "en", "default"),
            Themes = ["dark", "default", "light"]
        };
        var vm = new SettingsViewModel(fake, new NullDeploymentConfigStore());

        vm.Load(ProjectPath);

        await Assert.That(vm.AvailableThemes.Count).IsEqualTo(expectedThemeCount);
        await Assert.That(vm.AvailableThemes.Contains("default")).IsTrue();
        await Assert.That(vm.SelectedTheme).IsEqualTo("default");
    }

    [Test]
    public async Task SaveAsync_Basic_CallsSaveWithUpdatedTitle()
    {
        var fake = new FakeSiteSettingsService();
        var vm = new SettingsViewModel(fake, new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        vm.Title = "New Title";
        await vm.SaveCommand.ExecuteAsync(null);

        await Assert.That(fake.LastSavedSettings).IsNotNull();
        await Assert.That(fake.LastSavedSettings!.Title).IsEqualTo("New Title");
        await Assert.That(vm.StatusMessage).IsEqualTo("Settings saved.");
    }

    [Test]
    public async Task SaveAsync_Advanced_WritesRawYaml()
    {
        var fake = new FakeSiteSettingsService();
        var vm = new SettingsViewModel(fake, new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        vm.IsAdvanced = true;
        vm.RawYaml = "title: From Raw\nbaseUrl: http://changed/\n";
        await vm.SaveCommand.ExecuteAsync(null);

        await Assert.That(fake.RawYamlContent).IsEqualTo("title: From Raw\nbaseUrl: http://changed/\n");
        await Assert.That(vm.StatusMessage).IsEqualTo("Settings saved.");
    }

    [Test]
    public async Task IsAdvanced_SwitchToTrue_LoadsRawYamlFromService()
    {
        var fake = new FakeSiteSettingsService
        {
            RawYamlContent = "title: From File\n"
        };
        var vm = new SettingsViewModel(fake, new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        vm.IsAdvanced = true;

        await Assert.That(vm.RawYaml).IsEqualTo("title: From File\n");
    }

    [Test]
    public async Task IsAdvanced_SwitchBackToFalse_ReloadsBasicFields()
    {
        var fake = new FakeSiteSettingsService
        {
            CurrentSettings = new("Reloaded", "Desc", "http://localhost/", "en", "default")
        };
        var vm = new SettingsViewModel(fake, new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        vm.Title = "Dirty Title (not saved)";
        vm.IsAdvanced = true;
        vm.IsAdvanced = false;

        await Assert.That(vm.Title).IsEqualTo("Reloaded");
    }

    [Test]
    public async Task DeploymentVariants_ContainsAllValues()
    {
        var vm = new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore());

        const int expectedVariantCount = 4;
        await Assert.That(vm.DeploymentVariants.Count).IsEqualTo(expectedVariantCount);
        await Assert.That(vm.DeploymentVariants.Contains(DeploymentVariant.None)).IsTrue();
        await Assert.That(vm.DeploymentVariants.Contains(DeploymentVariant.GitHubPages)).IsTrue();
        await Assert.That(vm.DeploymentVariants.Contains(DeploymentVariant.AzureStaticWebApps)).IsTrue();
        await Assert.That(vm.DeploymentVariants.Contains(DeploymentVariant.Filesystem)).IsTrue();
    }

    [Test]
    public async Task FilesystemModes_ContainsAllValues()
    {
        var vm = new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore());

        const int expectedModeCount = 2;
        await Assert.That(vm.FilesystemModes.Count).IsEqualTo(expectedModeCount);
        await Assert.That(vm.FilesystemModes.Contains(FilesystemMode.PlainCopy)).IsTrue();
        await Assert.That(vm.FilesystemModes.Contains(FilesystemMode.Zip)).IsTrue();
    }

    [Test]
    public async Task Language_InvalidCode_ShowsWarning()
    {
        var vm = new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        vm.Language = "xx-bad!";

        await Assert.That(vm.LanguageWarning).IsNotNull();
        await Assert.That(vm.HasLanguageWarning).IsTrue();
    }

    [Test]
    public async Task Language_ValidDeDe_NoWarning()
    {
        var vm = new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        vm.Language = "de-DE";

        await Assert.That(vm.LanguageWarning).IsNull();
        await Assert.That(vm.HasLanguageWarning).IsFalse();
    }

    [Test]
    public async Task Language_Empty_NoWarning()
    {
        var vm = new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        vm.Language = string.Empty;

        await Assert.That(vm.LanguageWarning).IsNull();
        await Assert.That(vm.HasLanguageWarning).IsFalse();
    }

    [Test]
    public async Task Load_SetsStatusMessageToNull()
    {
        var fake = new FakeSiteSettingsService();
        var vm = new SettingsViewModel(fake, new NullDeploymentConfigStore());
        vm.Load(ProjectPath);

        await vm.SaveCommand.ExecuteAsync(null);
        vm.Load(ProjectPath);

        await Assert.That(vm.StatusMessage).IsNull();
        await Assert.That(vm.HasStatusMessage).IsFalse();
    }
}
