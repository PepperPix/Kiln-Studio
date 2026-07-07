namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeTaxonomyValueCache : ITaxonomyValueCache
{
    public Dictionary<string, List<string>> SuggestionsByTaxonomy { get; } = [];
    public List<(string ProjectPath, string TaxonomyName, IReadOnlyCollection<string> Values)> AddValuesCalls { get; } = [];

    public IReadOnlyList<string> GetSuggestions(string projectPath, string taxonomyName) =>
        SuggestionsByTaxonomy.TryGetValue(taxonomyName, out var values) ? values : [];

    public void Rebuild(string projectPath, IReadOnlyDictionary<string, IReadOnlyCollection<string>> valuesByTaxonomy)
    {
        foreach (var (name, values) in valuesByTaxonomy)
            SuggestionsByTaxonomy[name] = [.. values];
    }

    public void AddValues(string projectPath, string taxonomyName, IReadOnlyCollection<string> values)
    {
        AddValuesCalls.Add((projectPath, taxonomyName, values));
        if (!SuggestionsByTaxonomy.TryGetValue(taxonomyName, out var existing))
        {
            existing = [];
            SuggestionsByTaxonomy[taxonomyName] = existing;
        }

        foreach (var value in values)
        {
            if (!existing.Contains(value))
                existing.Add(value);
        }
    }
}
