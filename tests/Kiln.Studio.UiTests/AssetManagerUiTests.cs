namespace Kiln.Studio.UiTests;

using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

/// <summary>
/// PLAN-073: headless UI tests for the new Assets nav destination (Asset Manager).
/// </summary>
public sealed class AssetManagerUiTests
{
    private const string ReferencedAssetFileName = "referenced-photo.png";
    private const string OrphanAssetFileName = "orphan-photo.png";

    [Test]
    public async Task AssetsNav_Click_OpensAssetManagerView_NotPlaceholder()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");

            var vm = BuildShellViewModel(sitePath, storeDir);
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            vm.NavRail.SelectCommand.Execute(NavTarget.Assets);
            Dispatcher.UIThread.RunJobs();
            await Assert.That(vm.IsAssetsTargetSelected).IsTrue();

            var assetManager = window.GetVisualDescendants().OfType<AssetManagerView>().FirstOrDefault();
            await Assert.That(assetManager).IsNotNull();
            await Assert.That(assetManager!.IsEffectivelyVisible).IsTrue();

            var placeholder = window.GetVisualDescendants()
                .OfType<PlaceholderView>()
                .FirstOrDefault(p => p.Header?.ToString() == "Assets");
            await Assert.That(placeholder).IsNull();

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    [Test]
    public async Task AssetManager_ReferencedAsset_DisplaysReferenceLink()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            var postPath = SeedReferencedPost(sitePath);
            SeedLibraryAssets(sitePath);

            var vm = BuildShellViewModel(sitePath, storeDir);
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            SelectPost(vm, postPath);

            vm.NavRail.SelectCommand.Execute(NavTarget.Assets);
            Dispatcher.UIThread.RunJobs();

            await Assert.That(vm.AssetManager).IsNotNull();
            await Assert.That(vm.AssetManager!.Library).IsNotNull();

            var assetBrowser = window.GetVisualDescendants().OfType<AssetBrowserView>().First();
            var referencedEntry = assetBrowser.GetVisualDescendants()
                .OfType<TextBlock>()
                .First(tb => tb.Text == ReferencedAssetFileName);

            await Assert.That(referencedEntry).IsNotNull();

            var vmEntry = vm.AssetManager!.Library!.Entries.First(e => e.Name == ReferencedAssetFileName);
            await Assert.That(vmEntry.References).IsNotNull();
            await Assert.That(vmEntry.References!.Any(r => r.SourcePath == postPath)).IsTrue();

            var referenceLink = assetBrowser.GetVisualDescendants()
                .OfType<Button>()
                .FirstOrDefault(b =>
                    b.IsEffectivelyVisible &&
                    b.CommandParameter is AssetContentReference r &&
                    r.SourcePath == postPath);

            await Assert.That(referenceLink).IsNotNull();

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    [Test]
    public async Task AssetManager_ReferenceClick_NavigatesToContentEditor()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            var postPath = SeedReferencedPost(sitePath);
            SeedLibraryAssets(sitePath);

            var vm = BuildShellViewModel(sitePath, storeDir);
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            vm.NavRail.SelectCommand.Execute(NavTarget.Assets);
            Dispatcher.UIThread.RunJobs();

            await Assert.That(vm.AssetManager).IsNotNull();
            await Assert.That(vm.AssetManager!.Library).IsNotNull();

            var vmEntry = vm.AssetManager!.Library!.Entries.First(e => e.Name == ReferencedAssetFileName);
            await Assert.That(vmEntry.References).IsNotNull();
            await Assert.That(vmEntry.References!.Any(r => r.SourcePath == postPath)).IsTrue();

            var referenceLink = window.GetVisualDescendants()
                .OfType<Button>()
                .First(b =>
                    b.IsEffectivelyVisible &&
                    b.CommandParameter is AssetContentReference r &&
                    r.SourcePath == postPath);

            referenceLink.Command?.Execute(referenceLink.CommandParameter);
            Dispatcher.UIThread.RunJobs();

            await Assert.That(vm.IsContentTargetSelected).IsTrue();
            await Assert.That(vm.IsEditingContent).IsTrue();
            await Assert.That(Path.GetFullPath(vm.Editor.FilePath!)).IsEqualTo(Path.GetFullPath(postPath));

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    [Test]
    public async Task AssetManager_RenameReferencedAsset_RewritesBothPostBodies()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            var (postA, postB) = SeedTwoPostsReferencingAsset(sitePath);
            SeedLibraryAssets(sitePath);

            var vm = BuildShellViewModel(sitePath, storeDir, new FixedInputDialog("renamed-photo.png"));
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            vm.NavRail.SelectCommand.Execute(NavTarget.Assets);
            Dispatcher.UIThread.RunJobs();

            var renameButton = window.GetVisualDescendants()
                .OfType<Button>()
                .Where(b => b.IsEffectivelyVisible)
                .Where(b => b.Content is "Rename")
                .First(b =>
                    b.CommandParameter is AssetLibraryEntry e &&
                    e.Name == ReferencedAssetFileName);

            renameButton.Command!.Execute(renameButton.CommandParameter);
            Dispatcher.UIThread.RunJobs();

            await Assert.That(File.Exists(Path.Combine(sitePath, "static", "renamed-photo.png"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(sitePath, "static", ReferencedAssetFileName))).IsFalse();

            var bodyA = await File.ReadAllTextAsync(postA);
            var bodyB = await File.ReadAllTextAsync(postB);
            await Assert.That(bodyA).Contains("/assets/renamed-photo.png");
            await Assert.That(bodyB).Contains("/assets/renamed-photo.png");
            await Assert.That(bodyA).DoesNotContain("/assets/referenced-photo.png");
            await Assert.That(bodyB).DoesNotContain("/assets/referenced-photo.png");

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    private static ShellViewModel BuildShellViewModel(string sitePath, string storeDir, IInputDialog? inputDialog = null)
    {
        var engineHost = new EngineHost();
        var contentService = new ContentService();
        var dialog = inputDialog ?? new NullInputDialog();
        var assetManager = new AssetManagerViewModel(
            engineHost,
            new AssetLibraryService(),
            new NullFilePicker(),
            dialog,
            contentService,
            new ContentBodyReferenceRewriter(),
            new NullAssetThumbnailCache());

        return new ShellViewModel(
            new ProjectService(engineHost),
            new FixedFolderPicker(sitePath),
            dialog,
            new RecentProjectsStore(storeDir),
            contentService,
            new NullNewPageDialog(),
            new ProjectExplorerViewModel(),
            new EditorViewModel(contentService),
            new NullPreviewServer(),
            new NullBrowserLauncher(),
            new PreviewViewModel(),
            new NullBuildService(),
            new NullDeploymentService(),
            new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
            new NullDeploymentConfigStore(),
            new NullPublishService(),
            new FakeContentFrontmatterWriter(),
            assetManager);
    }



    private static void SelectPost(ShellViewModel vm, string postPath)
    {
        var post = vm.Explorer.Collections
            .SelectMany(c => c.FilteredEntries)
            .First(e => Path.GetFullPath(e.SourcePath) == Path.GetFullPath(postPath));

        vm.Explorer.SelectedEntry = post;
        Dispatcher.UIThread.RunJobs();
    }

    private static string SeedReferencedPost(string sitePath)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);
        var postPath = Path.Combine(postsDir, "referenced-post.md");

        const string content = """
            ---
            title: "Referenced Post"
            date: 2026-07-18
            draft: false
            ---

            ![Photo](/assets/referenced-photo.png)
            """;
        File.WriteAllText(postPath, content);
        return postPath;
    }

    private static (string PostA, string PostB) SeedTwoPostsReferencingAsset(string sitePath)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);
        var postA = Path.Combine(postsDir, "post-a.md");
        var postB = Path.Combine(postsDir, "post-b.md");

        const string content = """
            ---
            title: "Shared Asset Post"
            date: 2026-07-18
            draft: false
            ---

            ![Photo](/assets/referenced-photo.png)
            """;
        File.WriteAllText(postA, content);
        File.WriteAllText(postB, content);
        return (postA, postB);
    }

    private static void SeedLibraryAssets(string sitePath)
    {
        var staticDir = Path.Combine(sitePath, "static");
        Directory.CreateDirectory(staticDir);
        File.WriteAllText(Path.Combine(staticDir, ReferencedAssetFileName), "fake-png");
        File.WriteAllText(Path.Combine(staticDir, OrphanAssetFileName), "fake-png");
    }
}
