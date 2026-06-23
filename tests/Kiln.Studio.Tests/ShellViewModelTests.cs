namespace Kiln.Studio.Tests;

using Kiln.Services;
using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

public class ShellViewModelTests
{
    [Test]
    public async Task Title_IsKilnStudio()
    {
        var vm = new ShellViewModel();

        await Assert.That(vm.Title).IsEqualTo("Kiln Studio");
    }
}

public class EngineHostTests
{
    [Test]
    public async Task CreateProvider_ResolvesKilnCoreServices()
    {
        var host = new EngineHost();
        using var provider = host.CreateProvider("/tmp/test-project");

        await Assert.That(provider.GetRequiredService<IMarkdownProcessor>()).IsNotNull();
        await Assert.That(provider.GetRequiredService<ISiteBuilder>()).IsNotNull();
    }
}
