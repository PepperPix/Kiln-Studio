namespace Kiln.Studio.UiTests;

using Avalonia;
using Avalonia.Media.Imaging;

/// <summary>
/// Real (non-mocked) decode-path test for <see cref="AvaloniaImageDimensionReader"/> (PLAN-069):
/// generates an actual PNG file via Avalonia's own <see cref="RenderTargetBitmap"/> and verifies
/// the reader's <c>new Bitmap(stream).PixelSize</c> decode round-trips the real pixel dimensions.
/// </summary>
public sealed class AvaloniaImageDimensionReaderTests
{
    private const int TestImageWidth = 64;
    private const int TestImageHeight = 48;

    [Test]
    public async Task TryReadDimensions_RealPngFile_ReturnsActualPixelSize()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var pngPath = Path.Combine(tempDir, "test.png");
            using (var bitmap = new RenderTargetBitmap(new PixelSize(TestImageWidth, TestImageHeight)))
            using (var stream = File.Open(pngPath, FileMode.Create, FileAccess.Write))
            {
                bitmap.Save(stream, PngBitmapEncoderOptions.Default);
            }

            var reader = new AvaloniaImageDimensionReader();

            var dimensions = reader.TryReadDimensions(pngPath);

            await Assert.That(dimensions).IsNotNull();
            await Assert.That(dimensions!.Value.Width).IsEqualTo(TestImageWidth);
            await Assert.That(dimensions.Value.Height).IsEqualTo(TestImageHeight);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task TryReadDimensions_MissingFile_ReturnsNull()
    {
        var reader = new AvaloniaImageDimensionReader();
        var missingPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");

        var dimensions = reader.TryReadDimensions(missingPath);

        await Assert.That(dimensions).IsNull();
    }
}

