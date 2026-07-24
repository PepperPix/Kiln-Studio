namespace Kiln.Studio.TestSupport;

using Services;

public sealed class NullAssetThumbnailCache : IAssetThumbnailCache
{
    public string? GetOrCreateThumbnail(string? projectPath, string filePath, int targetSize) => null;
}
