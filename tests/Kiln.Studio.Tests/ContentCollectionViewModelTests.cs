namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;

public sealed class ContentCollectionViewModelTests
{
    private const int PostsEntryCount = 4;
    private const string GammaPath = "/posts/gamma.md";

    private static ContentCollectionViewModel MakePostsCollection()
    {
        var dto = new ContentCollectionDto(
            "posts",
            [
                new ContentEntry("Beta Post", "/posts/beta.md", false, new DateTime(2026, 6, 1)),
                new ContentEntry("Alpha Post", "/posts/alpha.md", true, new DateTime(2026, 1, 15)),
                new ContentEntry("Gamma Draft", GammaPath, true, null),
                new ContentEntry("Delta Final", "/posts/delta.md", false, new DateTime(2026, 3, 20)),
            ],
            "/tmp/content",
            ["tags"]);
        return new ContentCollectionViewModel(dto);
    }

    [Test]
    public async Task HasEntry_ReturnsTrue_ForExistingPath()
    {
        var vm = MakePostsCollection();

        await Assert.That(vm.HasEntry(GammaPath)).IsTrue();
    }

    [Test]
    public async Task HasEntry_ReturnsFalse_ForUnknownPath()
    {
        var vm = MakePostsCollection();

        await Assert.That(vm.HasEntry("/unknown.md")).IsFalse();
    }

    [Test]
    public async Task UpdateEntry_ChangesDraft_InDtoAndViewModel()
    {
        var vm = MakePostsCollection();
        var entry = vm.FilteredEntries.First(e => e.SourcePath == GammaPath);
        await Assert.That(entry.Draft).IsTrue();

        vm.UpdateEntry(GammaPath, false);

        await Assert.That(entry.Draft).IsFalse();
        await Assert.That(entry.ToggleDraftHeader).IsEqualTo("Mark as draft");
    }

    [Test]
    public async Task UpdateEntry_ApplyViewAfter_ReflectsNewDraftState()
    {
        var vm = MakePostsCollection();
        vm.ApplyView(null, DraftFilter.PublishedOnly, ContentSortMode.Default);

        var before = vm.FilteredEntries.ToList();
        await Assert.That(before.Any(e => e.SourcePath == GammaPath)).IsFalse();

        vm.UpdateEntry(GammaPath, false);
        vm.ApplyView(null, DraftFilter.PublishedOnly, ContentSortMode.Default);

        var after = vm.FilteredEntries.ToList();
        await Assert.That(after.Any(e => e.SourcePath == GammaPath)).IsTrue();
    }

    [Test]
    public async Task UpdateEntry_DoesNotThrow_ForUnknownPath()
    {
        var vm = MakePostsCollection();

        vm.UpdateEntry("/unknown.md", true);

        await Assert.That(vm.FilteredEntries.Count).IsEqualTo(PostsEntryCount);
    }
}
