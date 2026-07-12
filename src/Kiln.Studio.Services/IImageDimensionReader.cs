namespace Kiln.Studio.Services;

/// <summary>
/// Reads the pixel dimensions of an image file. Deliberately returns a nullable tuple rather than
/// throwing — any read failure (corrupt file, unsupported format) is purely informational and must
/// never block the asset upload flow (PLAN-069).
/// </summary>
public interface IImageDimensionReader
{
    (int Width, int Height)? TryReadDimensions(string filePath);
}
