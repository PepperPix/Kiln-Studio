namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;

public sealed class ProjectExplorerViewModelTests
{
    private const int CollectionCount = 2;
    private const int PostsEntryCount = 4;
    private const int PagesEntryCount = 2;
    private const int PostsDraftCount = 2;
    private const int PostsPublishedCount = 2;
    private const int ThirdIndex = 2;
    private const int LastPostIndex = 3;
    private const int KeystrokeIntervalMs = 10;

    // Generous relative to the 50ms debounce configured below (20x margin) — bumped from 1000ms
    // after this test was observed flaking under CI's parallel test execution (thread-pool
    // scheduling contention can occasionally delay the debounce continuation past a tighter
    // window). [Retry] on the test itself (below) is the primary safety net; this is belt-and-
    // suspenders.
    private const int SingleChangeSettleDelayMs = 2000;

    private static OpenedProject MakeProject()
    {
        var collection = new ContentCollectionDto(
            "posts",
            [
                new ContentEntry("Beta Post", "/posts/beta.md", false, new DateTime(2026, 6, 1)),
                new ContentEntry("Alpha Post", "/posts/alpha.md", true, new DateTime(2026, 1, 15)),
                new ContentEntry("Gamma Draft", "/posts/gamma.md", true, null),
                new ContentEntry("Delta Final", "/posts/delta.md", false, new DateTime(2026, 3, 20)),
            ],
            "/tmp/content",
            ["tags", "categories"]);

        var pages = new ContentCollectionDto(
            "pages",
            [
                new ContentEntry("About", "/pages/about.md", false, null),
                new ContentEntry("Contact", "/pages/contact.md", false, new DateTime(2026, 5, 10)),
            ],
            "/tmp/pages",
            []);

        return new OpenedProject("/tmp", "Test Site", [collection, pages]);
    }

    [Test]
    public async Task Load_PopulatesCollections()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        await Assert.That(vm.Collections.Count).IsEqualTo(CollectionCount);
        await Assert.That(vm.Collections[0].Name).IsEqualTo("posts");
        await Assert.That(vm.Collections[0].FilteredEntries.Count).IsEqualTo(PostsEntryCount);
    }

    [Test]
    public async Task Load_AppliesDefaultView_ShowsAllEntries()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsEntryCount);
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(PagesEntryCount);
    }

    [Test]
    public async Task SearchText_FiltersCollectionEntries()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SearchText = "Alpha";

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Alpha Post");
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(0);
    }

    [Test]
    public async Task SearchText_CaseInsensitive()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SearchText = "beta";

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Beta Post");
    }

    [Test]
    public async Task SearchText_NoMatch_ShowsEmptyCollections()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SearchText = "zzzzz";

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(0);
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(0);
    }

    [Test]
    public async Task DraftFilter_DraftsOnly_ShowsOnlyDrafts()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.DraftFilter = DraftFilter.DraftsOnly;

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsDraftCount);
        await Assert.That(vm.Collections[0].FilteredEntries.All(e => e.Draft)).IsTrue();
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(0);
    }

    [Test]
    public async Task DraftFilter_PublishedOnly()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.DraftFilter = DraftFilter.PublishedOnly;

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsPublishedCount);
        await Assert.That(vm.Collections[0].FilteredEntries.All(e => !e.Draft)).IsTrue();
    }

    [Test]
    public async Task SortMode_TitleAscending_ReordersEntries()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SortMode = ContentSortMode.TitleAscending;

        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Alpha Post");
        await Assert.That(vm.Collections[0].FilteredEntries[1].Title).IsEqualTo("Beta Post");
        await Assert.That(vm.Collections[0].FilteredEntries[ThirdIndex].Title).IsEqualTo("Delta Final");
        await Assert.That(vm.Collections[0].FilteredEntries[LastPostIndex].Title).IsEqualTo("Gamma Draft");
    }

    [Test]
    public async Task SortMode_TitleDescending()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SortMode = ContentSortMode.TitleDescending;

        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Gamma Draft");
        await Assert.That(vm.Collections[0].FilteredEntries[LastPostIndex].Title).IsEqualTo("Alpha Post");
    }

    [Test]
    public async Task Clear_ResetsSearchTextFilterAndSort()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SearchText = "Alpha";
        vm.DraftFilter = DraftFilter.DraftsOnly;
        vm.SortMode = ContentSortMode.TitleDescending;
        vm.Clear();

        await Assert.That(vm.SearchText).IsNull();
        await Assert.That(vm.DraftFilter).IsEqualTo(DraftFilter.All);
        await Assert.That(vm.SortMode).IsEqualTo(ContentSortMode.Default);
    }

    [Test]
    public async Task Clear_RemovesCollections()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.Clear();

        await Assert.That(vm.Collections.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ApplyToAll_SearchUpdatesAllCollections()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SearchText = "a";

        await Assert.That(vm.Collections[0].VisibleCount).IsGreaterThan(0);
        await Assert.That(vm.Collections[1].VisibleCount).IsGreaterThan(0);
    }

    [Test]
    public async Task ApplyToAll_AllEntriesStillAccessibleAfterFilter()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.SearchText = "Delta";

        await Assert.That(vm.Collections[0].FilteredEntries.Count).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].SourcePath).IsEqualTo("/posts/delta.md");
    }

    [Test]
    public async Task UpdateEntryDraft_ChangesDraftAndReappliesView()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        var entry = vm.Collections[0].FilteredEntries.First(e => e.SourcePath == "/posts/gamma.md");
        await Assert.That(entry.Draft).IsTrue();

        vm.UpdateEntryDraft("/posts/gamma.md", false);

        await Assert.That(entry.Draft).IsFalse();
        await Assert.That(entry.ToggleDraftHeader).IsEqualTo("Mark as draft");
    }

    [Test]
    public async Task UpdateEntryDraft_WithFilter_UpdatesVisibility()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());
        vm.DraftFilter = DraftFilter.PublishedOnly;

        await Assert.That(vm.Collections[0].FilteredEntries.Any(e => e.SourcePath == "/posts/gamma.md")).IsFalse();

        vm.UpdateEntryDraft("/posts/gamma.md", false);

        await Assert.That(vm.Collections[0].FilteredEntries.Any(e => e.SourcePath == "/posts/gamma.md")).IsTrue();
    }

    [Test]
    public async Task UpdateEntryDraft_DoesNotThrow_ForUnknownPath()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.UpdateEntryDraft("/unknown.md", true);

        await Assert.That(vm.Collections[0].FilteredEntries.Count).IsEqualTo(PostsEntryCount);
    }

    [Test]
    public async Task UpdateEntryDraft_PreservesSearchFilter()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());
        vm.SearchText = "Gamma";

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);

        vm.UpdateEntryDraft("/posts/gamma.md", false);

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].Draft).IsFalse();
    }

    [Test]
    public async Task Load_AfterClear_RecreatesCollections()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.Zero);
        vm.Load(MakeProject());

        vm.Clear();
        vm.Load(MakeProject());

        await Assert.That(vm.Collections.Count).IsEqualTo(CollectionCount);
        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsEntryCount);
    }

    [Test]
    public async Task SearchText_RapidKeystrokes_DebouncesApplyView()
    {
        const string typed = "Alpha";
        var vm = new ProjectExplorerViewModel(TimeSpan.FromMilliseconds(200));
        vm.Load(MakeProject());

        var applyCount = 0;
        foreach (var collection in vm.Collections)
        {
            collection.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ContentCollectionViewModel.VisibleCount))
                    applyCount++;
            };
        }

        foreach (var ch in typed)
        {
            vm.SearchText = (vm.SearchText ?? string.Empty) + ch;
            await Task.Delay(KeystrokeIntervalMs);
        }

        // 5 keystrokes across 2 collections would be 10 ApplyView calls without debounce.
        // The rapid typing above stays well within the 200ms debounce window, so the debounced
        // apply should not have fired for every keystroke yet.
        await Assert.That(applyCount).IsLessThan(typed.Length * vm.Collections.Count);

        // Let the debounce timer elapse so the final, coalesced apply runs. Polling avoids
        // flakes under CI thread-pool contention where Task.Delay(200) + UI-thread resume can
        // exceed a fixed 500ms wait.
        await WaitUntilAsync(
            () => vm.Collections[0].VisibleCount == 1,
            TimeSpan.FromSeconds(5));

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Alpha Post");
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        const int pollingIntervalMs = 50;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!predicate())
        {
            if (sw.Elapsed > timeout)
            {
                throw new TimeoutException($"Condition was not met within the {nameof(timeout)}.");
            }

            await Task.Delay(pollingIntervalMs);
        }
    }

    // Known flake under CI parallel test execution (thread-pool scheduling contention delaying the
    // debounce continuation) — retry rather than just padding the wait indefinitely.
    [Test]
    [Retry(2)]
    public async Task SearchText_SingleChange_StillAppliesAfterDebounceElapses()
    {
        var vm = new ProjectExplorerViewModel(TimeSpan.FromMilliseconds(50));
        vm.Load(MakeProject());

        vm.SearchText = "Delta";

        await Task.Delay(SingleChangeSettleDelayMs);

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Delta Final");
    }
}
