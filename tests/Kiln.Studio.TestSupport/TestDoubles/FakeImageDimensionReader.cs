namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeImageDimensionReader((int Width, int Height)? result) : IImageDimensionReader
{
    public string? LastFilePath { get; private set; }

    public (int Width, int Height)? TryReadDimensions(string filePath)
    {
        LastFilePath = filePath;
        return result;
    }
}
