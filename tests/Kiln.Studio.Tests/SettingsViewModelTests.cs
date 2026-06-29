namespace Kiln.Studio.Tests;

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
        var vm = new SettingsViewModel(fake);

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
        var vm = new SettingsViewModel(fake);

        vm.Load(ProjectPath);

        await Assert.That(vm.AvailableThemes.Count).IsEqualTo(expectedThemeCount);
        await Assert.That(vm.AvailableThemes.Contains("default")).IsTrue();
        await Assert.That(vm.SelectedTheme).IsEqualTo("default");
    }

    [Test]
    public async Task SaveAsync_Basic_CallsSaveWithUpdatedTitle()
    {
        var fake = new FakeSiteSettingsService();
        var vm = new SettingsViewModel(fake);
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
        var vm = new SettingsViewModel(fake);
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
        var vm = new SettingsViewModel(fake);
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
        var vm = new SettingsViewModel(fake);
        vm.Load(ProjectPath);

        vm.Title = "Dirty Title (not saved)";
        vm.IsAdvanced = true;
        vm.IsAdvanced = false;

        await Assert.That(vm.Title).IsEqualTo("Reloaded");
    }

    [Test]
    public async Task Load_SetsStatusMessageToNull()
    {
        var fake = new FakeSiteSettingsService();
        var vm = new SettingsViewModel(fake);
        vm.Load(ProjectPath);

        await vm.SaveCommand.ExecuteAsync(null);
        vm.Load(ProjectPath);

        await Assert.That(vm.StatusMessage).IsNull();
        await Assert.That(vm.HasStatusMessage).IsFalse();
    }
}
