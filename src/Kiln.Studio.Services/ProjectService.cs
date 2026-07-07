namespace Kiln.Studio.Services;

using Kiln.Models;
using Kiln.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed class ProjectService : IProjectService
{
    private readonly EngineHost _engineHost;
    private readonly ITaxonomyValueCache _taxonomyValueCache;

    public ProjectService(EngineHost engineHost, ITaxonomyValueCache? taxonomyValueCache = null)
    {
        _engineHost = engineHost;
        _taxonomyValueCache = taxonomyValueCache ?? new TaxonomyValueCache();
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
        var valuesByTaxonomy = new Dictionary<string, IReadOnlyCollection<string>>();

        var collections = config.Collections
            .Select(kv =>
            {
                var readItems = reader.ReadCollection(kv.Value, projectPath);
                CollectTaxonomyValues(readItems, valuesByTaxonomy);

                var entries = readItems
                    .Select(item => new ContentEntry(
                        item.Title,
                        item.SourcePath,
                        item.Draft,
                        item.Date))
                    .ToList();

                var contentDir = Path.IsPathRooted(kv.Value.Directory)
                    ? kv.Value.Directory
                    : Path.Combine(projectPath, kv.Value.Directory);

                return new ContentCollectionDto(kv.Key, entries, contentDir, [.. kv.Value.Taxonomies]);
            })
            .ToList();

        _taxonomyValueCache.Rebuild(projectPath, valuesByTaxonomy);

        return new OpenedProject(projectPath, config.Title, collections);
    }

    private static void CollectTaxonomyValues(
        IReadOnlyList<ContentItem> items,
        Dictionary<string, IReadOnlyCollection<string>> valuesByTaxonomy)
    {
        foreach (var item in items)
        {
            foreach (var (taxonomyName, rawValues) in item.Taxonomies)
            {
                if (rawValues is not IEnumerable<string> values)
                    continue;

                if (!valuesByTaxonomy.TryGetValue(taxonomyName, out var set) || set is not HashSet<string> hashSet)
                {
                    hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    valuesByTaxonomy[taxonomyName] = hashSet;
                }

                foreach (var value in values)
                    hashSet.Add(value);
            }
        }
    }

    public string CreateSite(string parentDirectory, string siteName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(siteName);
        if (!Directory.Exists(parentDirectory))
            throw new ArgumentException($"Directory does not exist: {parentDirectory}", nameof(parentDirectory));

        using var provider = _engineHost.CreateProvider(parentDirectory);
        var scaffolder = provider.GetRequiredService<IScaffolder>();
        var result = scaffolder.CreateSite(siteName, parentDirectory);
        return result.ProjectPath;
    }
}
