using Kiln.Services;
using Kiln.Studio.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kiln.Studio.Tests;

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