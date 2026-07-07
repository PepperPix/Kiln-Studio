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
}
