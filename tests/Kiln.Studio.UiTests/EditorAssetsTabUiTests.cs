namespace Kiln.Studio.UiTests;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

/// <summary>
/// PLAN-074: verifies the document-scoped Assets tab in the editor's unified right panel and the
/// site-wide asset Flyout opened from the Markdown toolbar.
/// </summary>
public sealed class EditorAssetsTabUiTests
{
    private const string PostTitle = "Assets Sample";
    private const string BundleAssetFileName = "bundle-photo.png";
    private const string LibraryAssetFileName = "library-photo.png";
    private const int AssetsTabIndex = 3;

    [Test]
    public async Task AssetsTab_ShowsDocumentScopedAssets_AndInsertAddsMarkdown()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            var postPath = SeedPageBundle(sitePath);

            var editor = new EditorViewModel(
                new ContentService(),
                filePicker: new FixedFilePicker());

            var vm = BuildShellViewModel(sitePath, storeDir, editor);
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await SelectPostAsync(vm, postPath);

            var rightPanelTabControl = window.GetVisualDescendants()
                .OfType<TabControl>()
                .First(tc => tc.Name == "RightPanelTabControl");
            rightPanelTabControl.SelectedIndex = AssetsTabIndex;
            Dispatcher.UIThread.RunJobs();

            var assetBrowser = window.GetVisualDescendants().OfType<AssetBrowserView>().FirstOrDefault();
            await Assert.That(assetBrowser).IsNotNull();
            await Assert.That(assetBrowser!.IsEffectivelyVisible).IsTrue();
            await Assert.That(vm.Editor.DocumentAssets).IsNotNull();
            await Assert.That(vm.Editor.DocumentAssets!.AssetChosen).IsNotNull();

            var assetEntry = vm.Editor.DocumentAssets.Entries.First(e => !e.IsFolder);
            var inserted = ClickFirstInsertButton(window, assetEntry);
            await Assert.That(inserted).IsTrue();

            var bodyEditor = window.GetVisualDescendants().OfType<TextEditor>().First(t => t.Name == "BodyEditor");
            await Assert.That(bodyEditor.Text).Contains($"![](./{BundleAssetFileName})");

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    [Test]
    public async Task AssetToolbarFlyout_ShowsSiteWideLibrary_AndInsertAddsMarkdown()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            var postPath = SeedFlatPost(sitePath);
            SeedLibraryAsset(sitePath);

            var editor = new EditorViewModel(
                new ContentService(),
                filePicker: new FixedFilePicker());

            var vm = BuildShellViewModel(sitePath, storeDir, editor);
            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await SelectPostAsync(vm, postPath);

            var assetButton = window.GetVisualDescendants()
                .OfType<Button>()
                .First(b => b.Name == "AssetButton");
            assetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();

            var flyoutBrowser = window.GetVisualDescendants().OfType<AssetBrowserView>().LastOrDefault();
            await Assert.That(flyoutBrowser).IsNotNull();
            await Assert.That(flyoutBrowser!.IsEffectivelyVisible).IsTrue();
            await Assert.That(vm.Editor.FlyoutAssets).IsNotNull();
            await Assert.That(vm.Editor.FlyoutAssets!.AssetChosen).IsNotNull();

            var assetEntry = vm.Editor.FlyoutAssets.Entries.First(e => !e.IsFolder);
            var inserted = ClickFirstInsertButton(window, assetEntry);
            await Assert.That(inserted).IsTrue();

            var bodyEditor = window.GetVisualDescendants().OfType<TextEditor>().First(t => t.Name == "BodyEditor");
            await Assert.That(bodyEditor.Text).Contains($"![](/assets/{LibraryAssetFileName})");

            // Flyout remains open after insert so multiple assets can be inserted in sequence.
            Dispatcher.UIThread.RunJobs();
            flyoutBrowser = window.GetVisualDescendants().OfType<AssetBrowserView>().LastOrDefault();
            await Assert.That(flyoutBrowser).IsNotNull();
            await Assert.That(flyoutBrowser!.IsEffectivelyVisible).IsTrue();

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static ShellViewModel BuildShellViewModel(string sitePath, string storeDir, EditorViewModel editor)
    {
        return new ShellViewModel(
            new ProjectService(new EngineHost()),
            new FixedFolderPicker(sitePath),
            new NullInputDialog(),
            new RecentProjectsStore(storeDir),
            new ContentService(),
            new NullNewPageDialog(),
            new ProjectExplorerViewModel(),
            editor,
            new NullPreviewServer(),
            new NullBrowserLauncher(),
            new PreviewViewModel(),
            new NullBuildService(),
            new NullDeploymentService(),
            new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
            new NullDeploymentConfigStore(),
            new NullPublishService(),
            new FakeContentFrontmatterWriter());
    }

    private static async Task SelectPostAsync(ShellViewModel vm, string postPath)
    {
        var post = vm.Explorer.Collections
            .SelectMany(c => c.FilteredEntries)
            .First(e => Path.GetFullPath(e.SourcePath) == Path.GetFullPath(postPath));

        vm.Explorer.SelectedEntry = post;
        await Assert.That(vm.Editor.HasDocument).IsTrue();
        Dispatcher.UIThread.RunJobs();
    }

    private static bool ClickFirstInsertButton(Window window, AssetLibraryEntry entry)
    {
        var insertButton = window.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b =>
                b.IsEffectivelyVisible &&
                b.Content?.ToString() == "Insert" &&
                b.CommandParameter is AssetLibraryEntry e &&
                e.RelativePath == entry.RelativePath);

        if (insertButton is null)
            return false;

        insertButton.Command?.Execute(insertButton.CommandParameter);
        Dispatcher.UIThread.RunJobs();
        return true;
    }

    private static string SeedPageBundle(string sitePath)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);
        var bundleDir = Path.Combine(postsDir, "assets-sample");
        Directory.CreateDirectory(bundleDir);
        var postPath = Path.Combine(bundleDir, "index.md");

        const string content = $"""
            ---
            title: "{PostTitle}"
            date: 2026-07-18
            draft: false
            ---

            Body of the assets sample post.
            """;
        File.WriteAllText(postPath, content);
        File.WriteAllText(Path.Combine(bundleDir, BundleAssetFileName), "fake-png");

        return postPath;
    }

    private static string SeedFlatPost(string sitePath)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);
        var postPath = Path.Combine(postsDir, "flyout-sample.md");

        const string content = $"""
            ---
            title: "Flyout Sample"
            date: 2026-07-18
            draft: false
            ---

            Body of the flyout sample post.
            """;
        File.WriteAllText(postPath, content);
        return postPath;
    }

    private static void SeedLibraryAsset(string sitePath)
    {
        var staticDir = Path.Combine(sitePath, "static");
        Directory.CreateDirectory(staticDir);
        File.WriteAllText(Path.Combine(staticDir, LibraryAssetFileName), "fake-png");
    }
}
