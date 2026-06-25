namespace Kiln.Studio.Tests;

using Kiln.Services;
using Kiln.Studio.Services;
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

public class ShellViewModelOpenTests
{
    private const string ReadyStatus = "Ready";

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
            var vm = new ShellViewModel(new ProjectService(new EngineHost()), picker, explorer);

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
        var vm = new ShellViewModel(
            new ProjectService(new EngineHost()),
            new NullFolderPicker(),
            explorer);

        await vm.OpenProjectCommand.ExecuteAsync(null);

        await Assert.That(vm.StatusMessage).IsEqualTo(ReadyStatus);
        await Assert.That(explorer.Collections.Count).IsEqualTo(0);
    }
}

file sealed class NullFolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}

file sealed class FixedFolderPicker(string path) : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(path);
}
