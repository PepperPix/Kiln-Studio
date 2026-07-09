namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeContentFrontmatterWriter : IContentFrontmatterWriter
{
    public string? LastSourcePath { get; private set; }
    public bool? LastSetDraft { get; private set; }
    public bool ToggleResult { get; set; }
    public Dictionary<string, IReadOnlyList<string>> StoredTaxonomyValues { get; } = [];

    public bool SetDraft(string sourcePath, bool draft)
    {
        LastSourcePath = sourcePath;
        LastSetDraft = draft;
        return draft;
    }

    public bool ToggleDraft(string sourcePath)
    {
        LastSourcePath = sourcePath;
        return ToggleResult;
    }

    public IReadOnlyList<string> GetTaxonomyValues(string sourcePath, string taxonomyName) =>
        StoredTaxonomyValues.TryGetValue(taxonomyName, out var values) ? values : [];

    public void SetTaxonomyValues(string sourcePath, string taxonomyName, IReadOnlyList<string> values) =>
        StoredTaxonomyValues[taxonomyName] = values;

    public Dictionary<string, string?> StoredScalarValues { get; } = [];

    public string? GetScalarValue(string sourcePath, string key) =>
        StoredScalarValues.TryGetValue(key, out var value) ? value : null;

    public void SetScalarValue(string sourcePath, string key, string? value) =>
        StoredScalarValues[key] = value;

    public string RemoveOwnedKeys(string frontMatterText, IReadOnlyCollection<string> keys) => frontMatterText;
}
