namespace Kiln.Studio.Services;

using Kiln.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Manages a Kiln engine provider for a specific project path.
/// </summary>
public sealed class EngineHost
{
    private readonly Action<IServiceCollection> _registerKilnServices;

    public EngineHost()
    {
        _registerKilnServices = services => services.AddKiln();
    }

    /// <summary>
    /// Creates a service provider with the Kiln engine registered for the given project path.
    /// </summary>
    public ServiceProvider CreateProvider(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var services = new ServiceCollection();
        _registerKilnServices(services);
        return services.BuildServiceProvider();
    }
}
