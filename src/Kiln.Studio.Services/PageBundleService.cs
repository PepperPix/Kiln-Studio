namespace Kiln.Studio.Services;

public sealed class PageBundleService : IPageBundleService
{
    private const string IndexFileName = "index.md";

    public bool IsPageBundle(string sourcePath) =>
        string.Equals(Path.GetFileName(sourcePath), IndexFileName, StringComparison.OrdinalIgnoreCase);

    public PageBundleUploadResult UploadAsset(string sourcePath, string uploadedFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(uploadedFilePath);

        if (IsPageBundle(sourcePath))
        {
            var bundleDir = Path.GetDirectoryName(sourcePath)!;
            var assetFileName = CopyIntoBundle(bundleDir, uploadedFilePath);
            return new PageBundleUploadResult(sourcePath, assetFileName, WasConverted: false);
        }

        var parentDir = Path.GetDirectoryName(sourcePath)!;
        var slug = Path.GetFileNameWithoutExtension(sourcePath);
        var targetDir = Path.Combine(parentDir, slug);

        if (Directory.Exists(targetDir))
        {
            throw new IOException(
                $"Cannot convert '{sourcePath}' into a page bundle: '{targetDir}' already exists.");
        }

        Directory.CreateDirectory(targetDir);
        var newSourcePath = Path.Combine(targetDir, IndexFileName);
        File.Move(sourcePath, newSourcePath);

        var relativeAssetFileName = CopyIntoBundle(targetDir, uploadedFilePath);
        return new PageBundleUploadResult(newSourcePath, relativeAssetFileName, WasConverted: true);
    }

    private static string CopyIntoBundle(string bundleDir, string uploadedFilePath)
    {
        var destination = FindUniqueFilePath(bundleDir, Path.GetFileName(uploadedFilePath));
        File.Copy(uploadedFilePath, destination);
        return Path.GetFileName(destination);
    }

    private static string FindUniqueFilePath(string dir, string fileName)
    {
        var candidate = Path.Combine(dir, fileName);
        if (!File.Exists(candidate))
            return candidate;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var counter = 2;
        while (true)
        {
            candidate = Path.Combine(dir, $"{nameWithoutExt}-{counter}{ext}");
            if (!File.Exists(candidate))
                return candidate;
            counter++;
        }
    }
}
