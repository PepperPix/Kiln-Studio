namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;

public class AssetManagerViewModelTests
{
    [Test]
    public async Task LoadProject_LibraryMode_ContainsStaticAssets()
    {
        var projectPath = await CreateSiteWithReferencedAssetAsync();
        try
        {
            var vm = MakeVm();
            vm.LoadProject(projectPath);

            await Assert.That(vm.Library).IsNotNull();
            await Assert.That(vm.Library!.Entries.Any(e => e.Name == "logo.png")).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task LoadProject_ByContentMode_GroupsOnlyItemsWithAssetDirectory()
    {
        var projectPath = await CreateSiteWithBundleAssetAsync();
        try
        {
            var vm = MakeVm();
            vm.LoadProject(projectPath);
            vm.ShowByContentCommand.Execute(null);

            await Assert.That(vm.ContentGroups.Count).IsEqualTo(1);
            await Assert.That(vm.ContentGroups[0].Title).IsEqualTo("Bundle Post");
            await Assert.That(vm.ContentGroups[0].Browser.Entries.Any(e => e.Name == "photo.png")).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task ShowOnlyOrphans_HidesReferencedLibraryAssets()
    {
        var projectPath = await CreateSiteWithReferencedAssetAsync();
        try
        {
            var vm = MakeVm();
            vm.LoadProject(projectPath);
            vm.ShowOnlyOrphans = true;

            await Assert.That(vm.Library!.Entries.Any(e => e.Name == "logo.png")).IsFalse();
            await Assert.That(vm.Library.Entries.Any(e => e.Name == "orphan.png")).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task ShowOnlyOrphans_Off_ShowsAllLibraryAssets()
    {
        var projectPath = await CreateSiteWithReferencedAssetAsync();
        try
        {
            var vm = MakeVm();
            vm.LoadProject(projectPath);

            await Assert.That(vm.Library!.Entries.Any(e => e.Name == "logo.png")).IsTrue();
            await Assert.That(vm.Library.Entries.Any(e => e.Name == "orphan.png")).IsTrue();
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    [Test]
    public async Task LoadProject_UsingProjectServiceCreateSite_FindsLibraryReferences()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);

        var projectService = new ProjectService(new EngineHost());
        var sitePath = projectService.CreateSite(parentDir, "my-blog");
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);
        var postPath = Path.Combine(postsDir, "referenced-post.md");

        await File.WriteAllTextAsync(postPath, """
            ---
            title: "Referenced Post"
            date: 2026-07-18
            draft: false
            ---

            ![Photo](/assets/referenced-photo.png)
            """);

        Directory.CreateDirectory(Path.Combine(sitePath, "static"));
        await File.WriteAllTextAsync(Path.Combine(sitePath, "static", "referenced-photo.png"), "fake-png");
        await File.WriteAllTextAsync(Path.Combine(sitePath, "static", "orphan-photo.png"), "fake-png");

        try
        {
            var vm = MakeVm();
            vm.LoadProject(sitePath);

            await Assert.That(vm.Library).IsNotNull();
            var entry = vm.Library!.Entries.FirstOrDefault(e => e.Name == "referenced-photo.png");
            await Assert.That(entry).IsNotNull();
            await Assert.That(entry!.References).IsNotNull();
            await Assert.That(entry.References!.Any(r => r.SourcePath == postPath)).IsTrue();
        }
        finally
        {
            Directory.Delete(parentDir, recursive: true);
        }
    }

    [Test]
    public async Task NavigateToContentItem_InvokesHostCallback()
    {
        var projectPath = await CreateSiteWithReferencedAssetAsync();
        try
        {
            var vm = MakeVm();
            var calledWith = string.Empty;
            vm.NavigateToContentItem = path => calledWith = path;
            vm.LoadProject(projectPath);

            await vm.Library!.NavigateToReferenceCommand.ExecuteAsync(new AssetContentReference("/posts/hello.md", "Hello"));

            await Assert.That(calledWith).IsEqualTo("/posts/hello.md");
        }
        finally
        {
            Directory.Delete(projectPath, recursive: true);
        }
    }

    private static AssetManagerViewModel MakeVm()
    {
        return new AssetManagerViewModel(
            new EngineHost(),
            new AssetLibraryService(),
            new NullFilePicker(),
            new NullInputDialog(),
            new ContentService(),
            new ContentBodyReferenceRewriter(),
            new NullAssetThumbnailCache());
    }

    private static async Task<string> CreateSiteWithReferencedAssetAsync()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "static"));
        Directory.CreateDirectory(Path.Combine(dir, "content", "posts"));

        await File.WriteAllTextAsync(Path.Combine(dir, "site.yaml"), """
            title: Test
            baseUrl: https://example.com
            collections:
              posts:
                name: posts
                content: content/posts
            """);

        await File.WriteAllTextAsync(Path.Combine(dir, "content", "posts", "hello.md"), """
            ---
            title: Hello
            date: 2026-07-20
            ---
            ![Logo](/assets/logo.png)
            """);

        await File.WriteAllTextAsync(Path.Combine(dir, "static", "logo.png"), "fake-png");
        await File.WriteAllTextAsync(Path.Combine(dir, "static", "orphan.png"), "fake-png");

        return dir;
    }

    private static async Task<string> CreateSiteWithBundleAssetAsync()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(dir, "content", "posts", "bundle-post"));

        await File.WriteAllTextAsync(Path.Combine(dir, "site.yaml"), """
            title: Test
            baseUrl: https://example.com
            collections:
              posts:
                name: posts
                content: content/posts
            """);

        await File.WriteAllTextAsync(Path.Combine(dir, "content", "posts", "bundle-post", "index.md"), """
            ---
            title: Bundle Post
            date: 2026-07-20
            ---
            ![Photo](./photo.png)
            """);

        await File.WriteAllTextAsync(Path.Combine(dir, "content", "posts", "bundle-post", "photo.png"), "fake-png");

        return dir;
    }
}
