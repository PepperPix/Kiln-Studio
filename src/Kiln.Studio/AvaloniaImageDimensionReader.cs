namespace Kiln.Studio;

using Avalonia.Media.Imaging;
using Kiln.Studio.Services;

/// <summary>
/// Avalonia-backed <see cref="IImageDimensionReader"/> implementation (PLAN-069). Lives in the
/// app project, not <c>Kiln.Studio.ViewModels</c>, because that layer is deliberately Avalonia-free
/// (see <c>AssetBrowserView</c>/<c>AssetBrowserViewModel</c> for the established interface/implementation split).
/// </summary>
public sealed class AvaloniaImageDimensionReader : IImageDimensionReader
{
    public (int Width, int Height)? TryReadDimensions(string filePath)
    {
        try
        {
            // Load the full file into memory first so the file handle is closed before Avalonia's
            // Bitmap potentially keeps a lazy-decoding reference open (observed as an IOException
            // on Windows when tests delete the containing directory immediately after reading).
            var bytes = File.ReadAllBytes(filePath);
            using var stream = new MemoryStream(bytes);
            using var bitmap = new Bitmap(stream);
            return (bitmap.PixelSize.Width, bitmap.PixelSize.Height);
        }
#pragma warning disable CA1031 // every decode failure (corrupt file, unsupported format, I/O error) is treated identically: no feedback
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }
}
