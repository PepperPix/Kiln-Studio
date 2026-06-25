namespace Kiln.Studio.Tests;

using Kiln.Services;
using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

file sealed class NullFolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}

file sealed class FixedFolderPicker(string path) : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(path);
}

file sealed class NullInputDialog : IInputDialog
{
    public Task<string?> PromptAsync(string title, string message) => Task.FromResult<string?>(null);
}

file sealed class FixedInputDialog(string response) : IInputDialog
{
    public Task<string?> PromptAsync(string title, string message) => Task.FromResult<string?>(response);
}

file sealed class NullNewPageDialog : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames) => Task.FromResult<NewPageRequest?>(null);
}

file sealed class FixedNewPageDialog(string collectionName, string title) : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames)
        => Task.FromResult<NewPageRequest?>(new NewPageRequest(collectionName, title));
}

public class ShellViewModelTests
{
    private const string SiteTitle = "Kiln Studio";

    private static (ShellViewModel vm, string storeDir) MakeVm(
        IFolderPicker folderPicker,
        IInputDialog inputDialog,
        ProjectExplorerViewModel? explorer = null)
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
            new EditorViewModel(new ContentService()));
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
                new EditorViewModel(new ContentService()));

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
                new EditorViewModel(new ContentService()));

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
                new EditorViewModel(new ContentService()));

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
                new EditorViewModel(new ContentService()));

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
                new EditorViewModel(new ContentService()));

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
}

