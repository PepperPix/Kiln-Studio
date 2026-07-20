namespace Kiln.Studio.UiTests;

using Avalonia.Media.Imaging;

/// <summary>
/// Real decode+scale test for <see cref="AvaloniaAssetThumbnailCache"/> (PLAN-073):
/// generates an actual PNG file and verifies the cached thumbnail is created at the requested
/// target boundary while preserving aspect ratio.
/// </summary>
public sealed class AvaloniaAssetThumbnailCacheTests
{
    private const int TestImageWidth = 200;
    private const int TestImageHeight = 100;
    private const int TargetSize = 96;
    private const int ExpectedAspectRatioDivisor = 2;

    [Test]
    public async Task GetOrCreateThumbnail_RealPngFile_CreatesScaledThumbnail()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pngPath = Path.Combine(tempDir, "test.png");
            using (var bitmap = new RenderTargetBitmap(new Avalonia.PixelSize(TestImageWidth, TestImageHeight)))
            using (var stream = File.Open(pngPath, FileMode.Create, FileAccess.Write))
            {
                bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            }

            var cache = new AvaloniaAssetThumbnailCache();
            var thumbnailPath = cache.GetOrCreateThumbnail(tempDir, pngPath, TargetSize);

            await Assert.That(thumbnailPath).IsNotNull();
            await Assert.That(File.Exists(thumbnailPath)).IsTrue();
            await Assert.That(thumbnailPath).StartsWith(Path.Combine(tempDir, ".kiln", "studio-thumbnails"));

            using var thumbnail = new Bitmap(thumbnailPath!);
            await Assert.That(thumbnail.PixelSize.Width).IsEqualTo(TargetSize);
            await Assert.That(thumbnail.PixelSize.Height).IsEqualTo(TargetSize / ExpectedAspectRatioDivisor);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task GetOrCreateThumbnail_MissingFile_ReturnsNull()
    {
        var cache = new AvaloniaAssetThumbnailCache();
        var missingPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");

        var thumbnailPath = cache.GetOrCreateThumbnail(null, missingPath, TargetSize);

        await Assert.That(thumbnailPath).IsNull();
    }

    [Test]
    public async Task GetOrCreateThumbnail_SecondCall_ReturnsExistingCachedFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pngPath = Path.Combine(tempDir, "test.png");
            using (var bitmap = new RenderTargetBitmap(new Avalonia.PixelSize(TestImageWidth, TestImageHeight)))
            using (var stream = File.Open(pngPath, FileMode.Create, FileAccess.Write))
            {
                bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            }

            var cache = new AvaloniaAssetThumbnailCache();
            var first = cache.GetOrCreateThumbnail(tempDir, pngPath, TargetSize);
            var second = cache.GetOrCreateThumbnail(tempDir, pngPath, TargetSize);

            await Assert.That(first).IsEqualTo(second);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
