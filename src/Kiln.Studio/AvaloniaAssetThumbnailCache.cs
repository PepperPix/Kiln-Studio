namespace Kiln.Studio;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Avalonia;
using Avalonia.Media.Imaging;
using Kiln.Studio.Services;

public sealed class AvaloniaAssetThumbnailCache : IAssetThumbnailCache
{
    public string? GetOrCreateThumbnail(string? projectPath, string filePath, int targetSize)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(filePath);
            var length = new FileInfo(filePath).Length;
            var uniqueString = $"{filePath}_{lastWrite.Ticks}_{length}_{targetSize}";

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(uniqueString));
            var hex = Convert.ToHexString(hashBytes);
            var cachedFileName = $"{hex}.png";

            var cacheDir = projectPath is not null
                ? Path.Combine(projectPath, ".kiln", "studio-thumbnails")
                : Path.Combine(Path.GetTempPath(), "kiln-studio-thumbnails");

            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            var cachedPath = Path.Combine(cacheDir, cachedFileName);

            if (File.Exists(cachedPath))
            {
                return cachedPath;
            }

            // Load, scale and save
            var bytes = File.ReadAllBytes(filePath);
            using var stream = new MemoryStream(bytes);
            using var bitmap = new Bitmap(stream);

            var origWidth = bitmap.PixelSize.Width;
            var origHeight = bitmap.PixelSize.Height;
            if (origWidth <= 0 || origHeight <= 0)
                return null;

            int newWidth;
            int newHeight;
            if (origWidth > origHeight)
            {
                newWidth = targetSize;
                newHeight = (int)Math.Max(1, (double)origHeight * targetSize / origWidth);
            }
            else
            {
                newHeight = targetSize;
                newWidth = (int)Math.Max(1, (double)origWidth * targetSize / origHeight);
            }

            using var scaled = bitmap.CreateScaledBitmap(new PixelSize(newWidth, newHeight));
            using (var destStream = File.OpenWrite(cachedPath))
            {
                scaled.Save(destStream, PngBitmapEncoderOptions.Default);
            }

            return cachedPath;
        }
#pragma warning disable CA1031 // Treat all decode failures gracefully
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }
}
