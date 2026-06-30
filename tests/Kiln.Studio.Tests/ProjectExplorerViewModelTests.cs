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
            "/tmp/content");

        var pages = new ContentCollectionDto(
            "pages",
            [
                new ContentEntry("About", "/pages/about.md", false, null),
                new ContentEntry("Contact", "/pages/contact.md", false, new DateTime(2026, 5, 10)),
            ],
            "/tmp/pages");

        return new OpenedProject("/tmp", "Test Site", [collection, pages]);
    }

    [Test]
    public async Task Load_PopulatesCollections()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        await Assert.That(vm.Collections.Count).IsEqualTo(CollectionCount);
        await Assert.That(vm.Collections[0].Name).IsEqualTo("posts");
        await Assert.That(vm.Collections[0].FilteredEntries.Count).IsEqualTo(PostsEntryCount);
    }

    [Test]
    public async Task Load_AppliesDefaultView_ShowsAllEntries()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsEntryCount);
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(PagesEntryCount);
    }

    [Test]
    public async Task SearchText_FiltersCollectionEntries()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.SearchText = "Alpha";

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Alpha Post");
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(0);
    }

    [Test]
    public async Task SearchText_CaseInsensitive()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.SearchText = "beta";

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Beta Post");
    }

    [Test]
    public async Task SearchText_NoMatch_ShowsEmptyCollections()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.SearchText = "zzzzz";

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(0);
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(0);
    }

    [Test]
    public async Task DraftFilter_DraftsOnly_ShowsOnlyDrafts()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.DraftFilter = DraftFilter.DraftsOnly;

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsDraftCount);
        await Assert.That(vm.Collections[0].FilteredEntries.All(e => e.Draft)).IsTrue();
        await Assert.That(vm.Collections[1].VisibleCount).IsEqualTo(0);
    }

    [Test]
    public async Task DraftFilter_PublishedOnly()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.DraftFilter = DraftFilter.PublishedOnly;

        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsPublishedCount);
        await Assert.That(vm.Collections[0].FilteredEntries.All(e => !e.Draft)).IsTrue();
    }

    [Test]
    public async Task SortMode_TitleAscending_ReordersEntries()
    {
        var vm = new ProjectExplorerViewModel();
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
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.SortMode = ContentSortMode.TitleDescending;

        await Assert.That(vm.Collections[0].FilteredEntries[0].Title).IsEqualTo("Gamma Draft");
        await Assert.That(vm.Collections[0].FilteredEntries[LastPostIndex].Title).IsEqualTo("Alpha Post");
    }

    [Test]
    public async Task Clear_ResetsSearchTextFilterAndSort()
    {
        var vm = new ProjectExplorerViewModel();
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
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.Clear();

        await Assert.That(vm.Collections.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ApplyToAll_SearchUpdatesAllCollections()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.SearchText = "a";

        await Assert.That(vm.Collections[0].VisibleCount).IsGreaterThan(0);
        await Assert.That(vm.Collections[1].VisibleCount).IsGreaterThan(0);
    }

    [Test]
    public async Task ApplyToAll_AllEntriesStillAccessibleAfterFilter()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.SearchText = "Delta";

        await Assert.That(vm.Collections[0].FilteredEntries.Count).IsEqualTo(1);
        await Assert.That(vm.Collections[0].FilteredEntries[0].SourcePath).IsEqualTo("/posts/delta.md");
    }

    [Test]
    public async Task UpdateEntryDraft_ChangesDraftAndReappliesView()
    {
        var vm = new ProjectExplorerViewModel();
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
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());
        vm.DraftFilter = DraftFilter.PublishedOnly;

        await Assert.That(vm.Collections[0].FilteredEntries.Any(e => e.SourcePath == "/posts/gamma.md")).IsFalse();

        vm.UpdateEntryDraft("/posts/gamma.md", false);

        await Assert.That(vm.Collections[0].FilteredEntries.Any(e => e.SourcePath == "/posts/gamma.md")).IsTrue();
    }

    [Test]
    public async Task UpdateEntryDraft_DoesNotThrow_ForUnknownPath()
    {
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.UpdateEntryDraft("/unknown.md", true);

        await Assert.That(vm.Collections[0].FilteredEntries.Count).IsEqualTo(PostsEntryCount);
    }

    [Test]
    public async Task UpdateEntryDraft_PreservesSearchFilter()
    {
        var vm = new ProjectExplorerViewModel();
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
        var vm = new ProjectExplorerViewModel();
        vm.Load(MakeProject());

        vm.Clear();
        vm.Load(MakeProject());

        await Assert.That(vm.Collections.Count).IsEqualTo(CollectionCount);
        await Assert.That(vm.Collections[0].VisibleCount).IsEqualTo(PostsEntryCount);
    }
}
