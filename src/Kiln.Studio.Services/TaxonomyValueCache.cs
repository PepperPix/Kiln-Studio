namespace Kiln.Studio.Services;

using System.Text.Json;

public sealed class TaxonomyValueCache : ITaxonomyValueCache
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public IReadOnlyList<string> GetSuggestions(string projectPath, string taxonomyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(taxonomyName);

        var cache = Load(projectPath);
        return cache.TryGetValue(taxonomyName, out var values) ? values : [];
    }

    public void Rebuild(string projectPath, IReadOnlyDictionary<string, IReadOnlyCollection<string>> valuesByTaxonomy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentNullException.ThrowIfNull(valuesByTaxonomy);

        var normalized = valuesByTaxonomy.ToDictionary(
            kvp => kvp.Key,
            kvp => Normalize(kvp.Value));

        Save(projectPath, normalized);
    }

    public void AddValues(string projectPath, string taxonomyName, IReadOnlyCollection<string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(taxonomyName);
        ArgumentNullException.ThrowIfNull(values);

        if (values.Count == 0)
            return;

        var cache = Load(projectPath).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        cache.TryGetValue(taxonomyName, out var existing);
        var merged = new SortedSet<string>(existing ?? [], StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                merged.Add(value);
        }

        cache[taxonomyName] = [.. merged];
        Save(projectPath, cache.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value));
    }

    private static IReadOnlyList<string> Normalize(IEnumerable<string> values)
    {
        var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                set.Add(value);
        }

        return [.. set];
    }

    private static Dictionary<string, IReadOnlyList<string>> Load(string projectPath)
    {
        var path = CachePath(projectPath);
        if (!File.Exists(path))
            return [];

        var json = File.ReadAllText(path);
        var raw = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
        return raw?.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value) ?? [];
    }

    private static void Save(string projectPath, Dictionary<string, IReadOnlyList<string>> cache)
    {
        var path = CachePath(projectPath);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(cache, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static string CachePath(string projectPath) =>
        Path.Combine(projectPath, ".kiln", "taxonomy-values.json");
}
