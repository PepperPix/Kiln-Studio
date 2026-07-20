namespace Kiln.Studio.Services;

/// <summary>
/// Generates and caches small thumbnail previews for asset files.
/// </summary>
public interface IAssetThumbnailCache
{
    /// <summary>
    /// Gets the path of the cached thumbnail for the given file and target size.
    /// Creates the thumbnail if it does not exist yet.
    /// </summary>
    /// <param name="projectPath">Optional project directory to store thumbnails under .kiln/studio-thumbnails/</param>
    /// <param name="filePath">The absolute path of the original image asset</param>
    /// <param name="targetSize">Target size (width/height boundary)</param>
    /// <returns>Absolute path to the thumbnail file, or null if generation fails</returns>
    string? GetOrCreateThumbnail(string? projectPath, string filePath, int targetSize);
}
