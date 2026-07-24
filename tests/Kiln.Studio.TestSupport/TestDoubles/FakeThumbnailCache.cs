namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeThumbnailCache : IAssetThumbnailCache
{
    public string? GetOrCreateThumbnail(string? projectPath, string filePath, int targetSize)
    {
        var name = Path.GetFileName(filePath);
        return $"/thumbs/{name}";
    }
}
