namespace Kiln.Studio.Services;

public enum AssetPickerDestination
{
    Library,
    PageBundle
}

/// <param name="Destination">
/// <see cref="AssetPickerDestination.Library"/>: <paramref name="Path"/> is already a path
/// relative to static/ (either browsed to an existing file, or produced by an upload the dialog
/// performed itself). <see cref="AssetPickerDestination.PageBundle"/>: <paramref name="Path"/> is
/// the raw, not-yet-copied local source path of the file the user picked for upload — the actual
/// copy/conversion happens afterwards via <see cref="IPageBundleService"/>.
/// </param>
public sealed record AssetPickerResult(AssetPickerDestination Destination, string Path);

/// <summary>
/// Dialog letting the user either browse the site-wide asset library or upload a new file
/// (ADR-050).
/// </summary>
public interface IAssetPickerDialog
{
    /// <param name="projectPath">Absolute path to the project root.</param>
    /// <param name="canUploadToPageBundle">
    /// Whether "insert into this page" is offered as an upload destination (requires a document
    /// to currently be open in the editor).
    /// </param>
    Task<AssetPickerResult?> ShowAsync(string projectPath, bool canUploadToPageBundle);
}
