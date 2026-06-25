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

public class ShellViewModelTests
{
    private const string SiteTitle = "Kiln Studio";

    [Test]
    public async Task Title_IsKilnStudio()
    {
        var explorer = new ProjectExplorerViewModel();
        var vm = new ShellViewModel(
            new ProjectService(new EngineHost()),
            new NullFolderPicker(),
            explorer);

        await Assert.That(vm.Title).IsEqualTo(SiteTitle);
    }

    [Test]
    public async Task OpenProject_NullPickerResult_DoesNotChangeStatus()
    {
        var explorer = new ProjectExplorerViewModel();
        var vm = new ShellViewModel(
            new ProjectService(new EngineHost()),
            new NullFolderPicker(),
            explorer);

        await vm.OpenProjectCommand.ExecuteAsync(null);

        await Assert.That(vm.StatusMessage).IsEqualTo("Ready");
        await Assert.That(explorer.Collections.Count).IsEqualTo(0);
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
