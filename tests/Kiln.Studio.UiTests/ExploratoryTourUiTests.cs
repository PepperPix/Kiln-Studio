namespace Kiln.Studio.UiTests;

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

/// <summary>
/// Exploratory "click-through" tour — NOT a regression/snapshot test (no baseline comparison,
/// no strict pass/fail on pixels). Seeds a realistic project with many posts, drives the Shell
/// through several real states via the same public commands/properties a user interaction would
/// hit, and dumps a PNG per step under Snapshots/__review__/ for manual visual review.
///
/// Re. headless TreeView virtualization (see ExplorerContextMenuUiTests): only the top-level
/// collection nodes are realized before any interaction. Explicitly toggling their IsExpanded
/// property (rather than relying on a simulated click on a not-yet-realized expander) does cause
/// child rows to materialize and render correctly in this test's screenshots.
/// </summary>
public sealed class ExploratoryTourUiTests
{
    private const int ManyPostsCount = 90;
    private const int SearchDebounceSettleDelayMs = 400;

    [Test]
    public async Task Tour_ManyPosts_CapturesKeyStates()
    {
        var reviewDir = ResolveReviewDir();
        Directory.CreateDirectory(reviewDir);

        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            SeedManyPosts(sitePath, ManyPostsCount);

            var explorer = new ProjectExplorerViewModel();
            var editor = new EditorViewModel(new ContentService());

            var vm = new ShellViewModel(
                projectService,
                new FixedFolderPicker(sitePath),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                explorer,
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

            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            // 1) Open the seeded project via the same public command a real "Open Folder" click uses.
            await vm.OpenProjectCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();
            await Assert.That(explorer.Collections.Sum(c => c.VisibleCount)).IsGreaterThanOrEqualTo(ManyPostsCount);

            Capture(window, reviewDir, "01_shell_opened_collapsed");

            // 2) Expand the collection nodes so their children realize and render. Note: only the
            //    top-level TreeViewItem containers (one per collection) are pre-realized before this
            //    call — child rows materialize as a side effect of setting IsExpanded here, and are
            //    visible in the resulting screenshot despite the low "topLevelContainers" count below.
            var topLevelContainers = TryExpandAllTreeViewItems(window);
            Capture(window, reviewDir, $"02_shell_expanded_posts_containers-{topLevelContainers}");

            // 3) Search-as-you-type against the full 90-post list. ProjectExplorerViewModel now
            //    debounces SearchText (~250ms default) before re-applying the filter, so wait for
            //    the debounce window to elapse before capturing the filtered/cleared states.
            explorer.SearchText = "post 7";
            await Task.Delay(SearchDebounceSettleDelayMs);
            Capture(window, reviewDir, "03_shell_search_filtered");

            // 3b) Isolate the deliberately overlong-titled post (seeded as post-001.md) to visually
            //     confirm CharacterEllipsis truncation on a real worst-case title.
            explorer.SearchText = "extraordinarily";
            await Task.Delay(SearchDebounceSettleDelayMs);
            Capture(window, reviewDir, "03b_shell_long_title_ellipsis");

            explorer.SearchText = null;
            await Task.Delay(SearchDebounceSettleDelayMs);

            // 4) Select an entry directly (equivalent to clicking a row) to load the editor + preview.
            var firstPost = explorer.Collections.First(c => c.Name == "posts").FilteredEntries.First();
            explorer.SelectedEntry = firstPost;
            Capture(window, reviewDir, "04_editor_with_content_loaded");

            // 5) Select the Preview tab in the unified right panel (ADR-056/PLAN-074) to visually
            //    inspect the Markdown preview rendering.
            const int previewTabIndex = 2;
            var rightPanelTabControl = window.GetVisualDescendants().OfType<TabControl>().First(tc => tc.Name == "RightPanelTabControl");
            rightPanelTabControl.SelectedIndex = previewTabIndex;
            Capture(window, reviewDir, "05_editor_with_preview_tab_selected");

            window.Close();
        }
        finally
        {
            if (Directory.Exists(parentDir)) Directory.Delete(parentDir, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void SeedManyPosts(string sitePath, int count)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);

        var baseDate = new DateOnly(2026, 1, 1);
        for (var i = 1; i <= count; i++)
        {
            var isDraft = i % 5 == 0;
            var date = baseDate.AddDays(i);
            // Post 1 gets a deliberately overlong title so the review screenshots can visually
            // confirm the entry-title CharacterEllipsis truncation (and its ToolTip) actually work.
            var title = i == 1
                ? $"Post {i}: an extraordinarily long title crafted specifically to overflow the " +
                  "content explorer column width and force visible character ellipsis truncation"
                : $"Post {i}: a sample entry about topic number {i}";
            var frontMatter = $"""
                ---
                title: "{title}"
                date: {date:yyyy-MM-dd}
                draft: {(isDraft ? "true" : "false")}
                ---
                """;
            var body = $"""

                This is the body of post {i}. It exists purely to populate the content
                explorer with a realistic number of entries for a manual UX review.
                """;
            File.WriteAllText(Path.Combine(postsDir, $"post-{i:000}.md"), frontMatter + body);
        }
    }

    /// <summary>
    /// Sets IsExpanded=true on every realized TreeViewItem container found in the visual tree.
    /// Returns how many containers were actually found/expanded (see class-level known limitation).
    /// </summary>
    private static int TryExpandAllTreeViewItems(Window window)
    {
        var treeView = window.GetVisualDescendants().OfType<TreeView>().FirstOrDefault();
        if (treeView is null)
            return 0;

        var items = treeView.GetVisualDescendants().OfType<TreeViewItem>().ToList();
        foreach (var item in items)
            item.IsExpanded = true;

        return items.Count;
    }

    private static void Capture(Window window, string reviewDir, string name)
    {
        WriteableBitmap frame = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException($"CaptureRenderedFrame returned null for '{name}'.");

        using var stream = File.Open(Path.Combine(reviewDir, $"{name}.png"), FileMode.Create, FileAccess.Write);
        frame.Save(stream, PngBitmapEncoderOptions.Default);
    }

    private static string ResolveReviewDir([System.Runtime.CompilerServices.CallerFilePath] string callerFile = "")
    {
        var testDir = Path.GetDirectoryName(callerFile)!;
        return Path.GetFullPath(Path.Combine(testDir, "Snapshots", "__review__"));
    }
}
