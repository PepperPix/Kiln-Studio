namespace Kiln.Studio.Services;

using Kiln.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed class MenuRefProvider : IMenuRefProvider
{
    private readonly EngineHost _engineHost;

    public MenuRefProvider(EngineHost engineHost)
    {
        _engineHost = engineHost;
    }

    public IReadOnlyList<string> GetCollectionRefs(string projectPath)
    {
        using var provider = _engineHost.CreateProvider(projectPath);
        var loader = provider.GetRequiredService<ISiteConfigLoader>();
        var config = loader.Load(projectPath);

        return config.Collections
            .Select(c => c.Key.TrimEnd('/') + "/")
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetItemRefs(string projectPath)
    {
        using var provider = _engineHost.CreateProvider(projectPath);
        var loader = provider.GetRequiredService<ISiteConfigLoader>();
        var reader = provider.GetRequiredService<IContentReader>();
        var config = loader.Load(projectPath);

        var refs = new List<string>();
        foreach (var (name, collection) in config.Collections)
        {
            var items = reader.ReadCollection(collection, projectPath);
            foreach (var item in items)
            {
                refs.Add($"{name}/{item.Slug}");
            }
        }

        return refs
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
