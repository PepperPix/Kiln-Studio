namespace Kiln.Studio.Tests;

using Services;
using TestSupport;
using ViewModels;

public class AssetBrowserViewModelTests
{
    private static string CreateTempProject()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "static", "images"));
        return dir;
    }

    [Test]
    public async Task Delete_WithoutReferenceIndex_DeletesSilently()
    {
        var projectPath = CreateTempProject();
        try
        {
            var filePath = Path.Combine(projectPath, "static", "readme.txt");
            await File.WriteAllTextAsync(filePath, "hello");

            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new NullFilePicker(),
                projectPath,
                Path.Combine(projectPath, "static"),
                isDocumentScoped: false);

            await vm.RefreshAsync();
            var entry = vm.Entries.First(e => !e.IsFolder);

            await vm.DeleteCommand.ExecuteAsync(entry);

            await Assert.That(File.Exists(filePath)).IsFalse();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task Delete_WithActiveReferences_IsBlocked()
    {
        var projectPath = CreateTempProject();
        try
        {
            var filePath = Path.Combine(projectPath, "static", "logo.png");
            await File.WriteAllTextAsync(filePath, "fake");

            var references = new Dictionary<string, IReadOnlyList<AssetContentReference>>(StringComparer.Ordinal)
            {
                ["/assets/logo.png"] = [new AssetContentReference("/posts/hello.md", "Hello")]
            };

            var notified = false;
            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new NullFilePicker(),
                projectPath,
                Path.Combine(projectPath, "static"),
                isDocumentScoped: false)
            {
                ReferenceIndex = references,
                NotifyDeleteBlocked = (_, _) =>
                {
                    notified = true;
                    return Task.CompletedTask;
                }
            };

            await vm.RefreshAsync();
            var entry = vm.Entries.First(e => e.Name == "logo.png");

            await vm.DeleteCommand.ExecuteAsync(entry);

            await Assert.That(File.Exists(filePath)).IsTrue();
            await Assert.That(notified).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task Rename_WithActiveReferences_RewritesReferences()
    {
        var projectPath = CreateTempProject();
        var postPath = Path.Combine(projectPath, "post.md");
        try
        {
            var filePath = Path.Combine(projectPath, "static", "logo.png");
            await File.WriteAllTextAsync(filePath, "fake");
            await File.WriteAllTextAsync(postPath, "![Logo](/assets/logo.png)");

            var references = new Dictionary<string, IReadOnlyList<AssetContentReference>>(StringComparer.Ordinal)
            {
                ["/assets/logo.png"] = [new AssetContentReference(postPath, "Hello")]
            };

            var rewritten = false;
            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new NullFilePicker(),
                projectPath,
                Path.Combine(projectPath, "static"),
                isDocumentScoped: false)
            {
                ReferenceIndex = references,
                PromptForRename = _ => Task.FromResult<string?>("logo-v2.png"),
                RewriteReferencesOnRename = (_, _, _) =>
                {
                    rewritten = true;
                    return Task.FromResult(true);
                }
            };

            await vm.RefreshAsync();
            var entry = vm.Entries.First(e => e.Name == "logo.png");

            await vm.RenameCommand.ExecuteAsync(entry);

            await Assert.That(rewritten).IsTrue();
            await Assert.That(File.Exists(Path.Combine(projectPath, "static", "logo-v2.png"))).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task DocumentScope_ExcludesIndexMd()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "content", "posts", "bundle"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "content", "posts", "bundle", "index.md"), "---\ntitle: Bundle\n---");
            await File.WriteAllTextAsync(Path.Combine(dir, "content", "posts", "bundle", "photo.png"), "fake");

            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new NullFilePicker(),
                dir,
                Path.Combine(dir, "content", "posts", "bundle"),
                isDocumentScoped: true);

            await vm.RefreshAsync();

            await Assert.That(vm.Entries.Any(e => e.Name == "index.md")).IsFalse();
            await Assert.That(vm.Entries.Any(e => e.Name == "photo.png")).IsTrue();
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Replace_OverwritesFileInPlace()
    {
        var projectPath = CreateTempProject();
        var sourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDir);
        try
        {
            var filePath = Path.Combine(projectPath, "static", "logo.png");
            var replacementPath = Path.Combine(sourceDir, "new-logo.png");
            await File.WriteAllTextAsync(filePath, "old");
            await File.WriteAllTextAsync(replacementPath, "new");

            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new FixedFilePicker(replacementPath),
                projectPath,
                Path.Combine(projectPath, "static"),
                isDocumentScoped: false);

            await vm.RefreshAsync();
            var entry = vm.Entries.First(e => e.Name == "logo.png");

            await vm.ReplaceCommand.ExecuteAsync(entry);

            await Assert.That(await File.ReadAllTextAsync(filePath)).IsEqualTo("new");
            await Assert.That(vm.Entries.Any(e => e.Name == "logo.png")).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
            Directory.Delete(sourceDir, recursive: true);
        }
    }

    [Test]
    public async Task DocumentScope_ProvidesThumbnails_WhenCacheAvailable()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "content", "posts", "bundle"));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "content", "posts", "bundle", "index.md"), "---\ntitle: Bundle\n---");
            await File.WriteAllTextAsync(Path.Combine(dir, "content", "posts", "bundle", "photo.png"), "fake");

            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new NullFilePicker(),
                dir,
                Path.Combine(dir, "content", "posts", "bundle"),
                isDocumentScoped: true)
            {
                ThumbnailCache = new FakeThumbnailCache()
            };

            await vm.RefreshAsync();
            var entry = vm.Entries.First(e => e.Name == "photo.png");

            await Assert.That(entry.ThumbnailSource).IsEqualTo("/thumbs/photo.png");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task EntryFilter_IsAppliedAfterRefresh()
    {
        var projectPath = CreateTempProject();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(projectPath, "static", "keep.txt"), "keep");
            await File.WriteAllTextAsync(Path.Combine(projectPath, "static", "skip.txt"), "skip");

            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new NullFilePicker(),
                projectPath,
                Path.Combine(projectPath, "static"),
                isDocumentScoped: false)
            {
                EntryFilter = entry => entry.Name != "skip.txt"
            };

            await vm.RefreshAsync();

            await Assert.That(vm.Entries.Any(e => e.Name == "keep.txt")).IsTrue();
            await Assert.That(vm.Entries.Any(e => e.Name == "skip.txt")).IsFalse();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task References_PopulatedForSiteScopedFiles_WhenReferenceIndexSet()
    {
        var projectPath = CreateTempProject();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(projectPath, "static", "logo.png"), "fake");

            var references = new Dictionary<string, IReadOnlyList<AssetContentReference>>(StringComparer.Ordinal)
            {
                ["/assets/logo.png"] = [new AssetContentReference("/posts/hello.md", "Hello")]
            };

            var vm = new AssetBrowserViewModel(
                new AssetLibraryService(),
                new NullFilePicker(),
                projectPath,
                Path.Combine(projectPath, "static"),
                isDocumentScoped: false)
            {
                ReferenceIndex = references
            };

            await vm.RefreshAsync();
            var entry = vm.Entries.First(e => e.Name == "logo.png");

            await Assert.That(entry.References).IsNotNull();
            await Assert.That(entry.References!.Count).IsEqualTo(1);
            await Assert.That(entry.References[0].Title).IsEqualTo("Hello");
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }
}
