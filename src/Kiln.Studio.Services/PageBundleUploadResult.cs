namespace Kiln.Studio.Services;

/// <param name="NewSourcePath">
/// The content item's file path after the upload. Unchanged from the input if it was already a
/// page bundle; points at the new index.md if a conversion happened.
/// </param>
/// <param name="RelativeAssetFileName">The uploaded file's name inside the bundle folder (no path).</param>
/// <param name="WasConverted"><see langword="true"/> if a flat file was converted into a page bundle.</param>
public sealed record PageBundleUploadResult(string NewSourcePath, string RelativeAssetFileName, bool WasConverted);
