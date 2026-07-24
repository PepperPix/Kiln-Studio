namespace Kiln.Studio.UiTests;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Services;
using TestSupport;
using ViewModels;
using Views;

/// <summary>
/// PLAN-080: headless UI tests for the Theme Manager nav destination.
/// </summary>
public sealed class ThemeManagerUiTests
{
    [Test]
    public async Task ThemeNav_Click_OpensThemeManagerView_NotPlaceholder()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");

            var vm = BuildShellViewModel(sitePath, storeDir);
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            vm.NavRail.SelectCommand.Execute(NavTarget.Theme);
            Dispatcher.UIThread.RunJobs();
            await Assert.That(vm.IsThemeTargetSelected).IsTrue();

            var themeManager = window.GetVisualDescendants().OfType<ThemeManagerView>().FirstOrDefault();
            await Assert.That(themeManager).IsNotNull();
            await Assert.That(themeManager!.IsEffectivelyVisible).IsTrue();

            var placeholder = window.GetVisualDescendants()
                .OfType<PlaceholderView>()
                .FirstOrDefault(p => p.Header?.ToString() == "Theme");
            await Assert.That(placeholder).IsNull();

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    [Test]
    public async Task ThemeManager_SelectFileInListBox_UpdatesPreview()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");

            var vm = BuildShellViewModel(sitePath, storeDir);
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            vm.NavRail.SelectCommand.Execute(NavTarget.Theme);
            Dispatcher.UIThread.RunJobs();

            await Assert.That(vm.ThemeManager).IsNotNull();

            var themeManager = window.GetVisualDescendants().OfType<ThemeManagerView>().First();
            var listBox = themeManager.GetVisualDescendants().OfType<ListBox>().First();
            await Assert.That(listBox.Items.Count).IsGreaterThan(0);

            var firstItem = listBox.ContainerFromIndex(0)!;
            listBox.SelectedIndex = 0;
            Dispatcher.UIThread.RunJobs();

            await Assert.That(vm.ThemeManager!.SelectedFile).IsNotNull();

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    private static ShellViewModel BuildShellViewModel(string sitePath, string storeDir)
    {
        var engineHost = new EngineHost();
        var contentService = new ContentService();
        var dialog = new NullInputDialog();
        var assetManager = new AssetManagerViewModel(
            engineHost,
            new AssetLibraryService(),
            new NullFilePicker(),
            dialog,
            contentService,
            new ContentBodyReferenceRewriter(),
            new NullAssetThumbnailCache());

        return new ShellViewModel(
            new ProjectService(engineHost),
            new FixedFolderPicker(sitePath),
            dialog,
            new RecentProjectsStore(storeDir),
            contentService,
            new NullNewPageDialog(),
            new ProjectExplorerViewModel(),
            new EditorViewModel(contentService),
            new NullPreviewServer(),
            new NullBrowserLauncher(),
            new PreviewViewModel(),
            new NullBuildService(),
            new NullDeploymentService(),
            new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
            new NullDeploymentConfigStore(),
            new NullPublishService(),
            new FakeContentFrontmatterWriter(),
            new MenuEditorViewModel(new NullMenuService(), new NullMenuRefProvider(), new NullInputDialog()),
            new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()),
            assetManager);
    }
}
