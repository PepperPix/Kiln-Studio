namespace Kiln.Studio.Tests;

using Kiln.Services;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

public class ProjectServiceTests
{
    private const string PostsCollection = "posts";

    [Test]
    public async Task Open_ValidKilnSite_ReturnsProjectWithCollections()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var host = new EngineHost();
            using var provider = host.CreateProvider(tempDir);
            var scaffolder = provider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("test-site", tempDir);
            var projectPath = scaffoldResult.ProjectPath;

            var service = new ProjectService(new EngineHost());
            var result = service.Open(projectPath);

            await Assert.That(result).IsNotNull();
            await Assert.That(result.Collections.Count).IsGreaterThan(0);

            var posts = result.Collections.FirstOrDefault(c => c.Name == PostsCollection);
            await Assert.That(posts).IsNotNull();
            await Assert.That(posts!.Entries.Count).IsGreaterThan(0);

            var firstEntry = posts.Entries[0];
            await Assert.That(File.Exists(firstEntry.SourcePath)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Open_MissingSiteYaml_ThrowsProjectOpenException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ProjectService(new EngineHost());

            await Assert.That(() => service.Open(tempDir))
                .Throws<ProjectOpenException>();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}

public class ProjectServiceCreateSiteTests
{
    private const string SiteName = "my-new-site";

    [Test]
    public async Task CreateSite_ValidArgs_ReturnsExistingPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ProjectService(new EngineHost());
            var projectPath = service.CreateSite(tempDir, SiteName);

            await Assert.That(Directory.Exists(projectPath)).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSite_ThenOpen_HasCollections()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ProjectService(new EngineHost());
            var projectPath = service.CreateSite(tempDir, SiteName);
            var opened = service.Open(projectPath);

            await Assert.That(opened.Collections.Count).IsGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSite_EmptySiteName_ThrowsArgumentException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ProjectService(new EngineHost());

            await Assert.That(() => service.CreateSite(tempDir, ""))
                .Throws<ArgumentException>();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreateSite_NonExistentParent_ThrowsArgumentException()
    {
        var service = new ProjectService(new EngineHost());

        await Assert.That(() => service.CreateSite("/no/such/directory", SiteName))
            .Throws<ArgumentException>();
    }
}

public class ShellViewModelOpenTests
{
    private const string ReadyStatus = "Ready";

    private static ShellViewModel MakeVm(
        IFolderPicker folderPicker,
        ProjectExplorerViewModel explorer,
        IRecentProjectsStore? store = null)
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        return new ShellViewModel(
            new ProjectService(new EngineHost()),
            folderPicker,
            new NullInputDialog(),
            store ?? new RecentProjectsStore(storeDir),
            new ContentService(),
            new NullNewPageDialog(),
            explorer,
            new EditorViewModel(new ContentService()),
            new FakePreviewServer(),
            new FakeBrowserLauncher(),
            new PreviewViewModel(),
            new FakeBuildService(),
            new FakeDeploymentService(),
            new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService());
    }

    [Test]
    public async Task OpenProject_ValidSite_FillsExplorerAndSetsStatus()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var host = new EngineHost();
            using var provider = host.CreateProvider(tempDir);
            var scaffolder = provider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("my-studio-site", tempDir);
            var projectPath = scaffoldResult.ProjectPath;

            var explorer = new ProjectExplorerViewModel();
            var picker = new FixedFolderPicker(projectPath);
            var vm = MakeVm(picker, explorer);

            await vm.OpenProjectCommand.ExecuteAsync(null);

            await Assert.That(vm.Explorer.Collections.Count).IsGreaterThan(0);
            await Assert.That(vm.StatusMessage).Contains("my-studio-site");
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task OpenProject_NullFromPicker_NoStatusChange()
    {
        var explorer = new ProjectExplorerViewModel();
        var vm = MakeVm(new NullFolderPicker(), explorer);

        await vm.OpenProjectCommand.ExecuteAsync(null);

        await Assert.That(vm.StatusMessage).IsEqualTo(ReadyStatus);
        await Assert.That(explorer.Collections.Count).IsEqualTo(0);
    }
}

