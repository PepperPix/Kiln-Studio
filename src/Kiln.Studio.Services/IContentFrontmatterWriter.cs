namespace Kiln.Studio.Services;

public interface IContentFrontmatterWriter
{
    bool SetDraft(string sourcePath, bool draft);

    bool ToggleDraft(string sourcePath);

    /// <summary>
    /// Reads the values of a single taxonomy field (e.g. "tags") from a content file's front matter.
    /// Returns an empty list when the file has no front matter, or the key is missing/empty.
    /// </summary>
    IReadOnlyList<string> GetTaxonomyValues(string sourcePath, string taxonomyName);

    /// <summary>
    /// Mutates a single taxonomy field in a content file's front matter (YAML sequence), leaving the
    /// rest of the document (other fields, comments, body) unchanged. An empty <paramref name="values"/>
    /// list removes the key entirely.
    /// </summary>
    void SetTaxonomyValues(string sourcePath, string taxonomyName, IReadOnlyList<string> values);

    /// <summary>
    /// Reads a single scalar front matter field (e.g. "title", "date", "description"). Returns
    /// <see langword="null"/> when the file has no front matter or the key is missing/empty.
    /// </summary>
    string? GetScalarValue(string sourcePath, string key);

    /// <summary>
    /// Mutates a single scalar front matter field, leaving the rest of the document unchanged.
    /// A <see langword="null"/> or empty <paramref name="value"/> removes the key entirely.
    /// </summary>
    void SetScalarValue(string sourcePath, string key, string? value);

    /// <summary>
    /// Removes the given top-level keys from a raw YAML front matter text block, for display in the
    /// editor's raw text area. The authoritative values for these keys are read/written via the
    /// dedicated Get/SetScalarValue and Get/SetTaxonomyValues methods, which operate directly on the
    /// file — this method only prevents them from also appearing (redundantly, out of sync with the
    /// structured editors) in the raw text the user sees/edits.
    /// </summary>
    string RemoveOwnedKeys(string frontMatterText, IReadOnlyCollection<string> keys);
}
