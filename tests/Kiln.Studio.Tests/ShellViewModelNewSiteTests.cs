using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;

namespace Kiln.Studio.Tests;

public class ShellViewModelNewSiteTests
{
    private const string NewSiteName = "testsite";

    [Test]
    public async Task NewSiteAsync_HappyPath_IsProjectOpenAndExplorerFilled()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var explorer = new ProjectExplorerViewModel();
            var store = new RecentProjectsStore(storeDir);
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog(NewSiteName),
                store,
                new ContentService(),
                new NullNewPageDialog(),
                explorer,
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm.NewSiteCommand.ExecuteAsync(null);

            await Assert.That(vm.IsProjectOpen).IsTrue();
            await Assert.That(vm.Explorer.Collections.Count).IsGreaterThan(0);
            await Assert.That(vm.RecentProjects.Count).IsEqualTo(1);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task NewSiteAsync_NullFolderPicker_NoChange()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new NullFolderPicker(),
                new FixedInputDialog(NewSiteName),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm.NewSiteCommand.ExecuteAsync(null);

            await Assert.That(vm.IsProjectOpen).IsFalse();
            await Assert.That(vm.RecentProjects.Count).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task NewSiteAsync_NullInputDialog_NoChange()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm.NewSiteCommand.ExecuteAsync(null);

            await Assert.That(vm.IsProjectOpen).IsFalse();
            await Assert.That(vm.RecentProjects.Count).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task OpenRecentAsync_OpensProjectFromRecentList()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var explorer = new ProjectExplorerViewModel();
            var store = new RecentProjectsStore(storeDir);
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog(NewSiteName),
                store,
                new ContentService(),
                new NullNewPageDialog(),
                explorer,
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            // Create the site first
            await vm.NewSiteCommand.ExecuteAsync(null);
            var projectPath = vm.CurrentProjectPath!;

            // Reset state to simulate reopening
            var explorer2 = new ProjectExplorerViewModel();
            var store2 = new RecentProjectsStore(storeDir);
            var vm2 = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new NullFolderPicker(),
                new NullInputDialog(),
                store2,
                new ContentService(),
                new NullNewPageDialog(),
                explorer2,
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await Assert.That(vm2.RecentProjects.Count).IsEqualTo(1);

            // Open via recent command
            await vm2.RecentProjects[0].OpenCommand.ExecuteAsync(null);

            await Assert.That(vm2.IsProjectOpen).IsTrue();
            await Assert.That(vm2.CurrentProjectPath).IsEqualTo(projectPath);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task CloseProject_AfterOpen_ResetsToWelcomeState()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var server = new FakePreviewServer();
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog(NewSiteName),
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
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            await vm.CloseProjectCommand.ExecuteAsync(null);

            await Assert.That(vm.IsProjectOpen).IsFalse();
            await Assert.That(vm.CurrentProjectPath).IsNull();
            await Assert.That(vm.CurrentProjectName).IsNull();
            await Assert.That(vm.Explorer.Collections.Count).IsEqualTo(0);
            await Assert.That(vm.StatusMessage).IsEqualTo("Ready");
            await Assert.That(server.StopCalled).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task CloseProject_WhenDirtyAndUserCancels_KeepsProjectOpen()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var contentFile = Path.Combine(tempParent, "test.md");
        await File.WriteAllTextAsync(contentFile, "---\ntitle: T\n---\n\nBody");

        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var dialog = new FixedUnsavedChangesDialog(UnsavedChangesDecision.Cancel);
        var vm = new ShellViewModel(
            new ProjectService(new EngineHost()),
            new FixedFolderPicker(tempParent),
            new FixedInputDialog(NewSiteName),
            new RecentProjectsStore(storeDir),
            new ContentService(),
            new NullNewPageDialog(),
            new ProjectExplorerViewModel(),
            new EditorViewModel(new ContentService()),
            new FakePreviewServer(),
            new FakeBrowserLauncher(),
            new PreviewViewModel(),
            new FakeBuildService(),
            new FakeDeploymentService(),
            new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
            new NullDeploymentConfigStore(),
            new NullPublishService(),
            new FakeContentFrontmatterWriter(),
            MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()),
            unsavedChangesDialog: dialog);
        try
        {
            await vm.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            vm.Editor.Load(contentFile);
            vm.Editor.Body = "changed";
            await Assert.That(vm.Editor.IsDirty).IsTrue();

            await vm.CloseProjectCommand.ExecuteAsync(null);

            await Assert.That(vm.IsProjectOpen).IsTrue();
            await Assert.That(dialog.Calls.Count).IsEqualTo(1);
            await Assert.That(dialog.Calls[0].AllowCancel).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task CloseProject_WhenDirtyAndUserChoosesSave_SavesThenCloses()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var contentFile = Path.Combine(tempParent, "test.md");
        await File.WriteAllTextAsync(contentFile, "---\ntitle: T\n---\n\nBody");

        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var dialog = new FixedUnsavedChangesDialog(UnsavedChangesDecision.Save);
        var vm = new ShellViewModel(
            new ProjectService(new EngineHost()),
            new FixedFolderPicker(tempParent),
            new FixedInputDialog(NewSiteName),
            new RecentProjectsStore(storeDir),
            new ContentService(),
            new NullNewPageDialog(),
            new ProjectExplorerViewModel(),
            new EditorViewModel(new ContentService()),
            new FakePreviewServer(),
            new FakeBrowserLauncher(),
            new PreviewViewModel(),
            new FakeBuildService(),
            new FakeDeploymentService(),
            new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
            new NullDeploymentConfigStore(),
            new NullPublishService(),
            new FakeContentFrontmatterWriter(),
            MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()),
            unsavedChangesDialog: dialog);
        try
        {
            await vm.NewSiteCommand.ExecuteAsync(null);
            vm.Editor.Load(contentFile);
            vm.Editor.Body = "changed";

            await vm.CloseProjectCommand.ExecuteAsync(null);

            await Assert.That(vm.IsProjectOpen).IsFalse();
            var written = await File.ReadAllTextAsync(contentFile);
            await Assert.That(written).Contains("changed");
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task SwitchRecent_SwitchesToDifferentProject()
    {
        var tempParent1 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var tempParent2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent1);
        Directory.CreateDirectory(tempParent2);
        Directory.CreateDirectory(storeDir);
        try
        {
            var store = new RecentProjectsStore(storeDir);

            // Create first site
            var vm1 = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent1),
                new FixedInputDialog("site-one"),
                store,
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new NullPreviewServer(),
                new NullBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm1.NewSiteCommand.ExecuteAsync(null);
            var path1 = vm1.CurrentProjectPath!;

            // Create second site
            var vm2 = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent2),
                new FixedInputDialog("site-two"),
                store,
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new NullPreviewServer(),
                new NullBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm2.NewSiteCommand.ExecuteAsync(null);

            // SwitchRecent back to first project
            await vm2.SwitchRecentCommand.ExecuteAsync(path1);

            await Assert.That(vm2.IsProjectOpen).IsTrue();
            await Assert.That(vm2.CurrentProjectPath).IsEqualTo(path1);
        }
        finally
        {
            if (Directory.Exists(tempParent1)) Directory.Delete(tempParent1, recursive: true);
            if (Directory.Exists(tempParent2)) Directory.Delete(tempParent2, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task CurrentProjectName_SetOnOpen()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog(NewSiteName),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new NullPreviewServer(),
                new NullBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm.NewSiteCommand.ExecuteAsync(null);

            await Assert.That(vm.CurrentProjectName).IsNotNull();
            await Assert.That(vm.CurrentProjectName).IsNotEmpty();
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }
}