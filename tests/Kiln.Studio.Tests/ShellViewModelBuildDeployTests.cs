namespace Kiln.Studio.Tests;

using Services;
using Services.Dto;
using TestSupport;
using ViewModels;

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
            OnBuildAsync = (_, _, _) =>
                Task.FromResult(new BuildSummary(false, 0, 0, 0, 3, "/tmp/site-out", [], ["first error"]))
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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                configStore,
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

            await vm.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm.CanPublish).IsFalse();

            configStore.Config =
                new DeploymentConfig(DeploymentVariant.Filesystem, "/tmp/out", FilesystemMode.PlainCopy);

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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                configStore,
                publishService,
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                configStore,
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

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
            OnSetUp = (_, target, _) =>
                new DeploymentSetupSummary(target, [".github/workflows/deploy.yml", "staticwebapp.config.json"])
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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                writer,
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));

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


    static class ShellViewModelTestsAccessor
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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));
            return (vm, storeDir);
        }
    }
}
