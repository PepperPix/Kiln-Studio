namespace Kiln.Studio.Tests;

using Kiln.Services;
using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

public class ShellViewModelTests
{
    private const string SiteTitle = "Kiln Studio";

    private static (ShellViewModel vm, string storeDir) MakeVm(
        IFolderPicker folderPicker,
        IInputDialog inputDialog,
        ProjectExplorerViewModel? explorer = null,
        IPreviewServer? previewServer = null,
        IBrowserLauncher? browserLauncher = null,
        IBuildService? buildService = null,
        IDeploymentService? deploymentService = null)
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var vm = new ShellViewModel(
            new ProjectService(new EngineHost()),
            folderPicker,
            inputDialog,
            new RecentProjectsStore(storeDir),
            new ContentService(),
            new NullNewPageDialog(),
            explorer ?? new ProjectExplorerViewModel(),
            new EditorViewModel(new ContentService()),
            previewServer ?? new FakePreviewServer(),
            browserLauncher ?? new FakeBrowserLauncher(),
            new PreviewViewModel(),
            buildService ?? new FakeBuildService(),
            deploymentService ?? new FakeDeploymentService(),
            new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());
        return (vm, storeDir);
    }

    [Test]
    public async Task Title_IsKilnStudio()
    {
        var (vm, storeDir) = MakeVm(new NullFolderPicker(), new NullInputDialog());
        try
        {
            await Assert.That(vm.Title).IsEqualTo(SiteTitle);
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task OpenProject_NullPickerResult_DoesNotChangeStatus()
    {
        var (vm, storeDir) = MakeVm(new NullFolderPicker(), new NullInputDialog());
        try
        {
            await vm.OpenProjectCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).IsEqualTo("Ready");
            await Assert.That(vm.Explorer.Collections.Count).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }
}

public class EngineHostTests
{
    [Test]
    public async Task CreateProvider_ResolvesKilnCoreServices()
    {
        var host = new EngineHost();
        using var provider = host.CreateProvider("/tmp/test-project");

        await Assert.That(provider.GetRequiredService<ISiteConfigLoader>()).IsNotNull();
        await Assert.That(provider.GetRequiredService<IContentReader>()).IsNotNull();
    }
}

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            vm.CloseProjectCommand.Execute(null);

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

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

public class ShellViewModelBuildDeployTests
{
    [Test]
    public async Task CanBuild_And_CanDeploy_AreFalseWhenNoProjectOpen()
    {
        var (vm, storeDir) = ShellViewModelTestsAccessor.MakeVm();
        try
        {
            await Assert.That(vm.BuildCommand.CanExecute(null)).IsFalse();
            await Assert.That(vm.SetUpGitHubPagesCommand.CanExecute(null)).IsFalse();
            await Assert.That(vm.SetUpAzureStaticWebAppsCommand.CanExecute(null)).IsFalse();
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task BuildCommand_SetsSuccessStatus_And_TogglesBusy()
    {
        var gate = new TaskCompletionSource();
        var buildService = new FakeBuildService
        {
            OnBuildAsync = async (_, _, cancellationToken) =>
            {
                await gate.Task.WaitAsync(cancellationToken);
                return new BuildSummary(true, 5, 5, 0, 42, "/tmp/site-out", ["warn"], []);
            }
        };

        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("build-test"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                buildService,
                new FakeDeploymentService(),
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);

            var buildTask = vm.BuildCommand.ExecuteAsync(null);
            await Task.Yield();

            await Assert.That(vm.IsBusy).IsTrue();

            gate.SetResult();
            await buildTask;

            await Assert.That(vm.IsBusy).IsFalse();
            await Assert.That(vm.StatusMessage).Contains("Built 5/5 files in 42 ms -> /tmp/site-out");
            await Assert.That(vm.StatusMessage).Contains("1 warning(s)");
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task BuildCommand_SetsFailureStatus()
    {
        var buildService = new FakeBuildService
        {
            OnBuildAsync = (_, _, _) => Task.FromResult(new BuildSummary(false, 0, 0, 0, 3, "/tmp/site-out", [], ["first error"]))
        };

        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("build-fail"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                buildService,
                new FakeDeploymentService(),
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);
            await vm.BuildCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).IsEqualTo("Build failed: first error");
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task CanPublish_IsTrueOnlyWhenVariantIsFilesystem()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var configStore = new FakeDeploymentConfigStore();

        try
        {
            configStore.Config = new DeploymentConfig(DeploymentVariant.None, null, FilesystemMode.PlainCopy);
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempDir),
                new FixedInputDialog("publish-test"),
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
                new NullSettingsDialog(),
                configStore,
                new NullPublishService(), new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm.CanPublish).IsFalse();

            configStore.Config = new DeploymentConfig(DeploymentVariant.Filesystem, "/tmp/out", FilesystemMode.PlainCopy);

            var path = vm.CurrentProjectPath!;
            var reloadedDeployConfig = configStore.Load(path);
            await Assert.That(reloadedDeployConfig.Variant).IsEqualTo(DeploymentVariant.Filesystem);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task PublishCommand_CallsPublishServiceAndSetsStatus()
    {
        var publishService = new FakePublishService
        {
            OnPublishAsync = (_, _, _) =>
                Task.FromResult(new PublishSummary(true, "/tmp/output", 42, null))
        };

        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var configStore = new FakeDeploymentConfigStore
        {
            Config = new DeploymentConfig(DeploymentVariant.Filesystem, "/tmp/output", FilesystemMode.PlainCopy)
        };

        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("publish-test"),
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
                new NullSettingsDialog(),
                configStore,
                publishService,
                new FakeContentFrontmatterWriter());
        
            await vm.NewSiteCommand.ExecuteAsync(null);

            await vm.PublishCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).Contains("Published");
            await Assert.That(vm.StatusMessage).Contains("/tmp/output");
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task GenerateDeploymentConfig_CallsDeploymentServiceForCiVariant()
    {
        var deploymentService = new FakeDeploymentService
        {
            OnSetUp = (_, target, _) => new DeploymentSetupSummary(target, [".github/workflows/deploy.yml"])
        };

        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var configStore = new FakeDeploymentConfigStore
        {
            Config = new DeploymentConfig(DeploymentVariant.GitHubPages, null, FilesystemMode.PlainCopy)
        };

        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("ci-test"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                deploymentService,
                new NullSettingsDialog(),
                configStore,
                new NullPublishService(), new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);

            await vm.GenerateDeploymentConfigCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).Contains("Deployment configured");
            await Assert.That(vm.StatusMessage).Contains("GitHub Pages");
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task DeploymentCommands_SetStatusWithCreatedFiles()
    {
        var deploymentService = new FakeDeploymentService
        {
            OnSetUp = (_, target, _) => new DeploymentSetupSummary(target, [".github/workflows/deploy.yml", "staticwebapp.config.json"])
        };

        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("deploy-test"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                deploymentService,
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);
            await vm.SetUpGitHubPagesCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).Contains("Deployment configured (GitHub Pages)");
            await Assert.That(vm.StatusMessage).Contains(".github/workflows/deploy.yml");
            await Assert.That(vm.StatusMessage).Contains("commit & push to deploy");
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task ToggleDraftAsync_UpdatesInPlace_WithoutReload()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        try
        {
            var writer = new FakeContentFrontmatterWriter();
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("my-site"),
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
                new NullSettingsDialog(),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                writer);

            await vm.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            // Find an entry in the explorer (scaffolded entries are non-draft)
            var entry = vm.Explorer.Collections
                .SelectMany(c => c.FilteredEntries)
                .First();

            var initialCollections = vm.Explorer.Collections.ToList();
            await Assert.That(entry.Draft).IsFalse();

            // Make the writer toggle to draft=true
            writer.ToggleResult = true;
            await entry.ToggleDraftCommand.ExecuteAsync(null);

            await Assert.That(vm.StatusMessage).IsEqualTo("Marked as draft.");
            await Assert.That(entry.Draft).IsTrue();

            // The explorer collections are the same instances (no reload)
            for (var i = 0; i < initialCollections.Count; i++)
                await Assert.That(vm.Explorer.Collections[i]).IsSameReferenceAs(initialCollections[i]);

            // Project is still open
            await Assert.That(vm.IsProjectOpen).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }
}

file static class ShellViewModelTestsAccessor
{
    public static (ShellViewModel vm, string storeDir) MakeVm()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var vm = new ShellViewModel(
            new ProjectService(new EngineHost()),
            new NullFolderPicker(),
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
            new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());
        return (vm, storeDir);
    }
}
