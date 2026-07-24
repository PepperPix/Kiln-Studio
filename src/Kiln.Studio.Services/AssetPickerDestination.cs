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
