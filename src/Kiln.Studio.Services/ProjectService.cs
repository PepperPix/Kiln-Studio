namespace Kiln.Studio.Services;

using Kiln.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed class ProjectService : IProjectService
{
    private readonly EngineHost _engineHost;

    public ProjectService(EngineHost engineHost)
    {
        _engineHost = engineHost;
    }

    public OpenedProject Open(string projectPath)
    {
        var siteYaml = Path.Combine(projectPath, "site.yaml");
        if (!File.Exists(siteYaml))
            throw new ProjectOpenException("No Kiln project found (site.yaml is missing).");

        using var provider = _engineHost.CreateProvider(projectPath);
        var loader = provider.GetRequiredService<ISiteConfigLoader>();
        var reader = provider.GetRequiredService<IContentReader>();

        var config = loader.Load(projectPath);

        var collections = config.Collections
            .Select(kv =>
            {
                var entries = reader.ReadCollection(kv.Value, projectPath)
                    .Select(item => new ContentEntry(
                        item.Title,
                        item.SourcePath,
                        item.Draft,
                        item.Date))
                    .ToList();
                return new ContentCollectionDto(kv.Key, entries);
            })
            .ToList();

        return new OpenedProject(projectPath, config.Title, collections);
    }
}
