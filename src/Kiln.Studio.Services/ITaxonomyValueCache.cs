namespace Kiln.Studio.Services;

/// <summary>
/// Derived, on-disk cache of taxonomy values already used somewhere in a project, keyed by
/// taxonomy name (e.g. "tags", "categories"). Used to power autocomplete suggestions in the
/// taxonomy chip input. Per ADR-011/ADR-047, the filesystem content is the single source of
/// truth — this cache is purely an accelerator and can always be rebuilt from it.
/// </summary>
public interface ITaxonomyValueCache
{
    /// <summary>
    /// Returns the known suggestion values for a single taxonomy of the given project.
    /// Returns an empty list if the project has no cache yet or the taxonomy is unknown.
    /// </summary>
    IReadOnlyList<string> GetSuggestions(string projectPath, string taxonomyName);

    /// <summary>
    /// Replaces the persisted cache for the given project with values freshly derived from the
    /// current content tree (e.g. right after opening a project). Intended to reflect the current
    /// filesystem truth exactly, not to merge with stale data.
    /// </summary>
    void Rebuild(string projectPath, IReadOnlyDictionary<string, IReadOnlyCollection<string>> valuesByTaxonomy);

    /// <summary>
    /// Incrementally adds newly-used values for a single taxonomy (e.g. after saving a content
    /// item). Never removes previously-known values.
    /// </summary>
    void AddValues(string projectPath, string taxonomyName, IReadOnlyCollection<string> values);
}
