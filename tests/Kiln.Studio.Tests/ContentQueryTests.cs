namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public sealed class ContentQueryTests
{
    private const int TotalEntries = 5;
    private const int PublishedCount = 3;
    private const int DraftCount = 2;
    private const int ImmediateAfterNewestIndex = 1;
    private const int TitleACount = 4;

    private static readonly ContentEntry Entry1 = new("Hello World", "/posts/hello.md", false, new DateTime(2026, 6, 15));
    private static readonly ContentEntry Entry2 = new("Another Post", "/posts/another.md", true, new DateTime(2026, 3, 20));
    private static readonly ContentEntry Entry3 = new("Draft Note", "/posts/draft.md", true, null);
    private static readonly ContentEntry Entry4 = new("Published Final", "/posts/final.md", false, new DateTime(2026, 6, 1));
    private static readonly ContentEntry Entry5 = new("Test Article", "/posts/test-article-for-search.md", false, new DateTime(2025, 12, 1));

    private static readonly IReadOnlyList<ContentEntry> TestEntries = [Entry1, Entry2, Entry3, Entry4, Entry5];

    [Test]
    public async Task Search_MatchesTitle_CaseInsensitive()
    {
        var result = ContentQuery.Apply(TestEntries, "hello", DraftFilter.All, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Hello World");
    }

    [Test]
    public async Task Search_MatchesFileName()
    {
        var result = ContentQuery.Apply(TestEntries, "test-article", DraftFilter.All, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Test Article");
    }

    [Test]
    public async Task Search_PartialMatchOnFileName()
    {
        var result = ContentQuery.Apply(TestEntries, "hello", DraftFilter.All, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].SourcePath).IsEqualTo("/posts/hello.md");
    }

    [Test]
    public async Task Search_NoMatch_ReturnsEmpty()
    {
        var result = ContentQuery.Apply(TestEntries, "zzzzz_nonexistent", DraftFilter.All, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Search_NullOrEmptyText_ReturnsAll()
    {
        var withNull = ContentQuery.Apply(TestEntries, null, DraftFilter.All, ContentSortMode.Default);
        var withEmpty = ContentQuery.Apply(TestEntries, string.Empty, DraftFilter.All, ContentSortMode.Default);

        await Assert.That(withNull.Count).IsEqualTo(TotalEntries);
        await Assert.That(withEmpty.Count).IsEqualTo(TotalEntries);
    }

    [Test]
    public async Task DraftFilter_PublishedOnly_ExcludesDrafts()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.PublishedOnly, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(PublishedCount);
        await Assert.That(result.All(e => !e.Draft)).IsTrue();
    }

    [Test]
    public async Task DraftFilter_DraftsOnly_ReturnsOnlyDrafts()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.DraftsOnly, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(DraftCount);
        await Assert.That(result.All(e => e.Draft)).IsTrue();
    }

    [Test]
    public async Task DraftFilter_All_ReturnsEverything()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.All, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(TotalEntries);
    }

    [Test]
    public async Task SortMode_Default_PreservesInputOrder()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.All, ContentSortMode.Default);

        await Assert.That(result[0].Title).IsEqualTo("Hello World");
        await Assert.That(result[ImmediateAfterNewestIndex].Title).IsEqualTo("Another Post");
        await Assert.That(result[DraftCount].Title).IsEqualTo("Draft Note");
        await Assert.That(result[PublishedCount].Title).IsEqualTo("Published Final");
        await Assert.That(result[TotalEntries - 1].Title).IsEqualTo("Test Article");
    }

    [Test]
    public async Task SortMode_DateNewest_OrdersByDateDescending()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.All, ContentSortMode.DateNewest);

        await Assert.That(result[0].Title).IsEqualTo("Hello World");
        await Assert.That(result[ImmediateAfterNewestIndex].Title).IsEqualTo("Published Final");
        await Assert.That(result[DraftCount].Title).IsEqualTo("Another Post");
        await Assert.That(result[PublishedCount].Title).IsEqualTo("Test Article");
        await Assert.That(result[TotalEntries - 1].Title).IsEqualTo("Draft Note");
    }

    [Test]
    public async Task SortMode_DateOldest_OrdersByDateAscending()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.All, ContentSortMode.DateOldest);

        await Assert.That(result[0].Title).IsEqualTo("Test Article");
        await Assert.That(result[ImmediateAfterNewestIndex].Title).IsEqualTo("Another Post");
        await Assert.That(result[DraftCount].Title).IsEqualTo("Published Final");
        await Assert.That(result[PublishedCount].Title).IsEqualTo("Hello World");
        await Assert.That(result[TotalEntries - 1].Title).IsEqualTo("Draft Note");
    }

    [Test]
    public async Task SortMode_TitleAscending_OrdersAlphabetically()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.All, ContentSortMode.TitleAscending);

        await Assert.That(result[0].Title).IsEqualTo("Another Post");
        await Assert.That(result[ImmediateAfterNewestIndex].Title).IsEqualTo("Draft Note");
        await Assert.That(result[DraftCount].Title).IsEqualTo("Hello World");
        await Assert.That(result[PublishedCount].Title).IsEqualTo("Published Final");
        await Assert.That(result[TotalEntries - 1].Title).IsEqualTo("Test Article");
    }

    [Test]
    public async Task SortMode_TitleDescending_OrdersReverseAlphabetically()
    {
        var result = ContentQuery.Apply(TestEntries, null, DraftFilter.All, ContentSortMode.TitleDescending);

        await Assert.That(result[0].Title).IsEqualTo("Test Article");
        await Assert.That(result[ImmediateAfterNewestIndex].Title).IsEqualTo("Published Final");
        await Assert.That(result[DraftCount].Title).IsEqualTo("Hello World");
        await Assert.That(result[PublishedCount].Title).IsEqualTo("Draft Note");
        await Assert.That(result[TotalEntries - 1].Title).IsEqualTo("Another Post");
    }

    [Test]
    public async Task SearchAndDraftFilter_Combine()
    {
        var result = ContentQuery.Apply(TestEntries, "final", DraftFilter.PublishedOnly, ContentSortMode.Default);

        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Title).IsEqualTo("Published Final");
    }

    [Test]
    public async Task SearchAndSort_Combine()
    {
        var result = ContentQuery.Apply(TestEntries, "a", DraftFilter.All, ContentSortMode.TitleAscending);

        await Assert.That(result.Count).IsEqualTo(TitleACount);
        await Assert.That(result[0].Title).IsEqualTo("Another Post");
        await Assert.That(result[TitleACount - 1].Title).IsEqualTo("Test Article");
    }
}
