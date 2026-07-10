namespace Kiln.Studio.Services;

/// <summary>
/// Site-wide asset library backed directly by the project's static/ folder (ADR-050). No cache —
/// every call reflects the current filesystem state.
/// </summary>
public interface IAssetLibraryService
{
    /// <summary>Lists files and subfolders directly under static/&lt;relativeFolder&gt;.</summary>
    /// <param name="projectPath">Absolute path to the project root.</param>
    /// <param name="relativeFolder">Path relative to static/, using forward slashes. Empty string for the root.</param>
    IReadOnlyList<AssetLibraryEntry> ListFolder(string projectPath, string relativeFolder);

    /// <summary>Creates a new subfolder named <paramref name="name"/> under static/&lt;relativeFolder&gt;.</summary>
    void CreateFolder(string projectPath, string relativeFolder, string name);

    /// <summary>
    /// Copies <paramref name="sourceFilePath"/> into static/&lt;relativeFolder&gt;/, resolving
    /// filename collisions with a numeric suffix (like <c>ContentService.FindUniquePath</c>).
    /// Returns the resulting path relative to static/ (e.g. "images/photo-2.png").
    /// </summary>
    string Upload(string projectPath, string relativeFolder, string sourceFilePath);
}
