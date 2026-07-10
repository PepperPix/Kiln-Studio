namespace Kiln.Studio.Services;

/// <summary>
/// Copies uploaded asset files into a content item's page bundle folder, converting a flat
/// content file into a bundle first if needed (ADR-050).
/// </summary>
public interface IPageBundleService
{
    /// <summary>True if sourcePath's file name is "index.md" (Kiln's page bundle detection).</summary>
    bool IsPageBundle(string sourcePath);

    /// <summary>
    /// If <paramref name="sourcePath"/> is already a page bundle, copies
    /// <paramref name="uploadedFilePath"/> into its directory. Otherwise converts it first:
    /// creates a new folder named after the file's slug (file name without extension) next to
    /// <paramref name="sourcePath"/>, MOVES <paramref name="sourcePath"/> into it as "index.md",
    /// then copies <paramref name="uploadedFilePath"/> into the same folder. Throws if the target
    /// folder already exists (no silent overwrite of a foreign folder). Filename collisions
    /// inside the bundle folder are resolved with a numeric suffix.
    /// </summary>
    PageBundleUploadResult UploadAsset(string sourcePath, string uploadedFilePath);
}
