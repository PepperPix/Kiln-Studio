namespace Kiln.Studio;

using Avalonia.Media.Imaging;
using Kiln.Studio.Services;

/// <summary>
/// Avalonia-backed <see cref="IImageDimensionReader"/> implementation (PLAN-069). Lives in the
/// app project, not <c>Kiln.Studio.ViewModels</c>, because that layer is deliberately Avalonia-free
/// (see <c>AvaloniaAssetPickerDialog</c> for the established interface/implementation split).
/// </summary>
public sealed class AvaloniaImageDimensionReader : IImageDimensionReader
{
    public (int Width, int Height)? TryReadDimensions(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
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
