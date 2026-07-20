namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class NullAssetThumbnailCache : IAssetThumbnailCache
{
    public string? GetOrCreateThumbnail(string? projectPath, string filePath, int targetSize) => null;
}
