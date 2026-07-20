namespace Kiln.Studio.Services;

/// <summary>
/// A single file or subfolder directly under a folder in the site-wide asset library (static/).
/// </summary>
/// <param name="Name">The file or folder name, without any path.</param>
/// <param name="IsFolder"><see langword="true"/> if this entry is a subfolder.</param>
/// <param name="RelativePath">Path relative to static/, using forward slashes (e.g. "images/photo.png").</param>
public sealed record AssetLibraryEntry(string Name, bool IsFolder, string RelativePath)
{
    /// <summary>
    /// Optional absolute path to a generated thumbnail preview (studio-side, never engine).
    /// </summary>
    public string? ThumbnailSource { get; init; }

    /// <summary>
    /// Optional list of content items referencing this library asset (site scope only).
    /// </summary>
    public IReadOnlyList<AssetContentReference>? References { get; init; }
}
