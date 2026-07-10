namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public class PageBundleServiceTests
{
    [Test]
    public async Task IsPageBundle_IndexMdFile_ReturnsTrue()
    {
        var service = new PageBundleService();

        await Assert.That(service.IsPageBundle("/some/dir/index.md")).IsTrue();
    }

    [Test]
    public async Task IsPageBundle_FlatFile_ReturnsFalse()
    {
        var service = new PageBundleService();

        await Assert.That(service.IsPageBundle("/some/dir/my-post.md")).IsFalse();
    }

    [Test]
    public async Task UploadAsset_AlreadyBundle_CopiesWithoutMovingOrConverting()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var bundleDir = Path.Combine(tempDir, "my-post");
        Directory.CreateDirectory(bundleDir);
        var sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDir);
        try
        {
            var indexPath = Path.Combine(bundleDir, "index.md");
            await File.WriteAllTextAsync(indexPath, "---\ntitle: My Post\n---\n\nBody");

            var uploadedFile = Path.Combine(sourceDir, "photo.png");
            await File.WriteAllTextAsync(uploadedFile, "fake-png");

            var service = new PageBundleService();
            var result = service.UploadAsset(indexPath, uploadedFile);

            await Assert.That(result.WasConverted).IsFalse();
            await Assert.That(result.NewSourcePath).IsEqualTo(indexPath);
            await Assert.That(result.RelativeAssetFileName).IsEqualTo("photo.png");
            await Assert.That(File.Exists(Path.Combine(bundleDir, "photo.png"))).IsTrue();
            await Assert.That(File.Exists(indexPath)).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Test]
    public async Task UploadAsset_FlatFile_ConvertsToBundle_MovesOriginal_AndCopiesUpload()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDir);
        try
        {
            var flatFilePath = Path.Combine(tempDir, "my-post.md");
            await File.WriteAllTextAsync(flatFilePath, "---\ntitle: My Post\n---\n\nBody");

            var uploadedFile = Path.Combine(sourceDir, "photo.png");
            await File.WriteAllTextAsync(uploadedFile, "fake-png");

            var service = new PageBundleService();
            var result = service.UploadAsset(flatFilePath, uploadedFile);

            var bundleDir = Path.Combine(tempDir, "my-post");
            await Assert.That(result.WasConverted).IsTrue();
            await Assert.That(result.NewSourcePath).IsEqualTo(Path.Combine(bundleDir, "index.md"));
            await Assert.That(result.RelativeAssetFileName).IsEqualTo("photo.png");
            await Assert.That(File.Exists(flatFilePath)).IsFalse();
            await Assert.That(File.Exists(Path.Combine(bundleDir, "index.md"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(bundleDir, "photo.png"))).IsTrue();

            var movedContent = await File.ReadAllTextAsync(Path.Combine(bundleDir, "index.md"));
            await Assert.That(movedContent).Contains("title: My Post");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Test]
    public async Task UploadAsset_TargetBundleFolderAlreadyExists_Throws()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "my-post")); // foreign folder already there
        var sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDir);
        try
        {
            var flatFilePath = Path.Combine(tempDir, "my-post.md");
            await File.WriteAllTextAsync(flatFilePath, "Body");

            var uploadedFile = Path.Combine(sourceDir, "photo.png");
            await File.WriteAllTextAsync(uploadedFile, "fake-png");

            var service = new PageBundleService();

            await Assert.That(() => service.UploadAsset(flatFilePath, uploadedFile))
                .ThrowsExactly<IOException>();

            // No silent overwrite: original file must still be untouched at its old path.
            await Assert.That(File.Exists(flatFilePath)).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Test]
    public async Task UploadAsset_FileNameCollisionInsideBundle_ResolvesWithNumericSuffix()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var bundleDir = Path.Combine(tempDir, "my-post");
        Directory.CreateDirectory(bundleDir);
        var sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDir);
        try
        {
            var indexPath = Path.Combine(bundleDir, "index.md");
            await File.WriteAllTextAsync(indexPath, "Body");
            await File.WriteAllTextAsync(Path.Combine(bundleDir, "photo.png"), "existing-png");

            var uploadedFile = Path.Combine(sourceDir, "photo.png");
            await File.WriteAllTextAsync(uploadedFile, "fake-png");

            var service = new PageBundleService();
            var result = service.UploadAsset(indexPath, uploadedFile);

            await Assert.That(result.RelativeAssetFileName).IsEqualTo("photo-2.png");
            await Assert.That(File.Exists(Path.Combine(bundleDir, "photo-2.png"))).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }
}
