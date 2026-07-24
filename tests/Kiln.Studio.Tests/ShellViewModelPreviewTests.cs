namespace Kiln.Studio.Tests;

using Services;
using TestSupport;
using ViewModels;

public class ShellViewModelPreviewTests
{
    [Test]
    public async Task CanServe_IsFalseWhenNoProjectOpen()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var server = new FakePreviewServer();
        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new NullFolderPicker(),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                server,
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy());

            await Assert.That(vm.StartFullPreviewCommand.CanExecute(null)).IsFalse();
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task StartFullPreview_SetsIsServingAndOpensBrowser()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var server = new FakePreviewServer();
        var browser = new FakeBrowserLauncher();
        try
        {
            const string siteName = "previewtest";
            var vm2 = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempDir),
                new FixedInputDialog(siteName),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                server,
                browser,
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy());

            await vm2.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm2.IsProjectOpen).IsTrue();

            await vm2.StartFullPreviewCommand.ExecuteAsync(null);

            await Assert.That(vm2.Preview.IsServing).IsTrue();
            await Assert.That(browser.LastOpened).IsNotNull();
            await Assert.That(browser.LastOpened).IsEqualTo(FakePreviewServer.FakeUri);
            await Assert.That(vm2.Preview.ServeStatus).Contains(FakePreviewServer.FakeUri.ToString());
            await Assert.That(server.IsRunning).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task StopFullPreview_SetsIsServingFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var server = new FakePreviewServer();
        var browser = new FakeBrowserLauncher();
        try
        {
            const string siteName = "stoptest";
            var vm2 = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempDir),
                new FixedInputDialog(siteName),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                server,
                browser,
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy());

            await vm2.NewSiteCommand.ExecuteAsync(null);
            await vm2.StartFullPreviewCommand.ExecuteAsync(null);
            await Assert.That(vm2.Preview.IsServing).IsTrue();

            vm2.StopFullPreviewCommand.Execute(null);

            await Assert.That(vm2.Preview.IsServing).IsFalse();
            await Assert.That(server.StopCalled).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task OpenProject_StopsRunningPreviewServer()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var server = new FakePreviewServer();
        var browser = new FakeBrowserLauncher();
        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempDir),
                new FixedInputDialog("mysite"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                server,
                browser,
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy());

            await vm.NewSiteCommand.ExecuteAsync(null);
            await vm.StartFullPreviewCommand.ExecuteAsync(null);
            await Assert.That(vm.Preview.IsServing).IsTrue();

            // Re-open the same project via recent entry, which triggers OpenPathAsync → StopFullPreview
            await vm.RecentProjects[0].OpenCommand.ExecuteAsync(null);

            await Assert.That(vm.Preview.IsServing).IsFalse();
            await Assert.That(server.StopCalled).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }
}
