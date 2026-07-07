using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Kiln.Studio.UiTests;

/// <summary>
/// Tolerance-based PNG snapshot comparer (ADR-030).
/// Renders a window frame and compares it against a baseline PNG.
/// Diff artifacts are written to Snapshots/__diff__/ on failure.
/// Set KILN_UPDATE_SNAPSHOTS=1 to re-baseline instead of comparing.
/// </summary>
internal static class SnapshotComparer
{
    /// <summary>Maximum fraction of differing pixels (0.1 %).</summary>
    private const double DefaultTolerance = 0.001;

    /// <summary>Per-channel delta considered "same pixel" — absorbs sub-pixel AA drift.</summary>
    private const int ChannelThreshold = 3;

    private const double PercentageFactor = 100.0;
    private const int BytesPerPixel = 4;

    // BGRA byte-order channel offsets
    private const int ChannelB = 0;
    private const int ChannelG = 1;
    private const int ChannelR = 2;
    private const int ChannelA = 3;

    // Diff image BGRA colours (red highlight on near-black background)
    private const byte DiffHighlightB = 0;
    private const byte DiffHighlightG = 0;
    private const byte DiffHighlightR = 220;
    private const byte DiffAlpha = 255;
    private const byte DiffBgB = 28;
    private const byte DiffBgG = 24;
    private const byte DiffBgR = 22;

    /// <summary>
    /// Captures the window frame and asserts it matches the named baseline,
    /// or writes a new baseline when KILN_UPDATE_SNAPSHOTS=1.
    /// </summary>
    public static async Task AssertMatchesBaseline(
        Window window,
        string name,
        double tolerance = DefaultTolerance,
        [CallerFilePath] string callerFile = "")
    {
        WriteableBitmap frame = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException(
                $"CaptureRenderedFrame returned null for snapshot '{name}'.");

        var snapshotDir = ResolveSnapshotDir(callerFile);
        var baselinePath = Path.Combine(snapshotDir, $"{name}.png");

        if (ShouldUpdateBaselines() || !File.Exists(baselinePath))
        {
            SaveBitmap(frame, baselinePath);
            await Task.CompletedTask;
            return;
        }

        var (diffFraction, diffMask) = CompareWithBaseline(frame, baselinePath);
        if (diffFraction > tolerance)
        {
            var diffDir = Path.Combine(snapshotDir, "__diff__");
            Directory.CreateDirectory(diffDir);

            var actualPath = Path.Combine(diffDir, $"{name}_actual.png");
            var diffPath = Path.Combine(diffDir, $"{name}_diff.png");

            SaveBitmap(frame, actualPath);
            WriteDiffImage(diffMask!, frame.PixelSize.Width, frame.PixelSize.Height, diffPath);

            throw new SnapshotMismatchException(
                $"Snapshot '{name}' exceeds tolerance: " +
                $"{diffFraction * PercentageFactor:F3}% differing pixels " +
                $"(max {tolerance * PercentageFactor:F3}%). " +
                $"Actual: {actualPath}  Diff: {diffPath}");
        }

        await Task.CompletedTask;
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private static string ResolveSnapshotDir(string callerFile)
    {
        var testDir = Path.GetDirectoryName(callerFile)!;
        var platform = ResolvePlatformFolderName();
        var dir = Path.GetFullPath(Path.Combine(testDir, "Snapshots", platform));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Baselines are nested per-OS (ADR-043) rather than shared, since it is not yet established
    /// whether Avalonia.Headless+Skia renders pixel-identically across platforms even with a
    /// bundled font and fixed DPI.
    /// </summary>
    private static string ResolvePlatformFolderName()
    {
        if (OperatingSystem.IsMacOS()) return "macos";
        if (OperatingSystem.IsWindows()) return "windows";
        if (OperatingSystem.IsLinux()) return "linux";
        throw new PlatformNotSupportedException("Unknown platform for snapshot baselines.");
    }

    private static bool ShouldUpdateBaselines() =>
        string.Equals(
            Environment.GetEnvironmentVariable("KILN_UPDATE_SNAPSHOTS"),
            "1",
            StringComparison.Ordinal);

    private static void SaveBitmap(WriteableBitmap bitmap, string path)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
        bitmap.Save(stream);
    }

    private static (double DiffFraction, bool[]? DiffMask) CompareWithBaseline(
        WriteableBitmap actual,
        string baselinePath)
    {
        int width = actual.PixelSize.Width;
        int height = actual.PixelSize.Height;
        const int bytesPerPixel = BytesPerPixel;
        int stride = width * bytesPerPixel;
        int totalPixels = width * height;
        int bufSize = totalPixels * bytesPerPixel;

        byte[] actualBytes = new byte[bufSize];
        byte[] baselineBytes = new byte[bufSize];

        ExtractPixels(actual, actualBytes, width, height, stride);

        using var baselineStream = File.OpenRead(baselinePath);
        using var baseline = new Bitmap(baselineStream);

        if (baseline.PixelSize.Width != width || baseline.PixelSize.Height != height)
            return (1.0, null);

        ExtractPixels(baseline, baselineBytes, width, height, stride);

        int diffCount = 0;
        var diffMask = new bool[totalPixels];
        for (int i = 0; i < totalPixels; i++)
        {
            int off = i * bytesPerPixel;
            bool differs =
                Math.Abs(actualBytes[off + ChannelB] - baselineBytes[off + ChannelB]) > ChannelThreshold ||
                Math.Abs(actualBytes[off + ChannelG] - baselineBytes[off + ChannelG]) > ChannelThreshold ||
                Math.Abs(actualBytes[off + ChannelR] - baselineBytes[off + ChannelR]) > ChannelThreshold;

            if (differs)
            {
                diffMask[i] = true;
                diffCount++;
            }
        }

        return ((double)diffCount / totalPixels, diffMask);
    }

    private static void ExtractPixels(Bitmap bitmap, byte[] dest, int width, int height, int stride)
    {
        var handle = GCHandle.Alloc(dest, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(
                new Avalonia.PixelRect(0, 0, width, height),
                handle.AddrOfPinnedObject(),
                dest.Length,
                stride);
        }
        finally
        {
            handle.Free();
        }
    }

    private static void WriteDiffImage(bool[] diffMask, int width, int height, string path)
    {
        using var wb = new WriteableBitmap(
            new Avalonia.PixelSize(width, height),
            new Avalonia.Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var locked = wb.Lock())
        {
            const int bytesPerPixel = BytesPerPixel;
            int stride = locked.RowBytes;
            int bufSize = height * stride;
            var buffer = new byte[bufSize];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixIdx = y * width + x;
                    int byteIdx = y * stride + x * bytesPerPixel;
                    if (diffMask[pixIdx])
                    {
                        buffer[byteIdx + ChannelB] = DiffHighlightB;
                        buffer[byteIdx + ChannelG] = DiffHighlightG;
                        buffer[byteIdx + ChannelR] = DiffHighlightR;
                        buffer[byteIdx + ChannelA] = DiffAlpha;
                    }
                    else
                    {
                        buffer[byteIdx + ChannelB] = DiffBgB;
                        buffer[byteIdx + ChannelG] = DiffBgG;
                        buffer[byteIdx + ChannelR] = DiffBgR;
                        buffer[byteIdx + ChannelA] = DiffAlpha;
                    }
                }
            }

            Marshal.Copy(buffer, 0, locked.Address, bufSize);
        }

        using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
        wb.Save(stream);
    }
}

/// <summary>Thrown when a snapshot exceeds the permitted tolerance.</summary>
internal sealed class SnapshotMismatchException(string message) : Exception(message);
