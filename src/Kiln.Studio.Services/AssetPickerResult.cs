namespace Kiln.Studio.Services;

public enum AssetPickerDestination
{
    Library,
    PageBundle,

    /// <summary>
    /// The asset already lives in the current content item's page bundle and only needs a relative
    /// reference inserted into the body. Path is relative to the bundle directory.
    /// </summary>
    PageBundleExisting,
}

/// <param name="Destination">
/// <see cref="AssetPickerDestination.Library"/>: <paramref name="Path"/> is already a path
/// relative to static/ (either browsed to an existing file, or produced by an upload the dialog
/// performed itself). <see cref="AssetPickerDestination.PageBundle"/>: <paramref name="Path"/> is
/// the raw, not-yet-copied local source path of the file the user picked for upload — the actual
/// copy/conversion happens afterwards via <see cref="IPageBundleService"/>.
/// </param>
public sealed record AssetPickerResult(AssetPickerDestination Destination, string Path);
