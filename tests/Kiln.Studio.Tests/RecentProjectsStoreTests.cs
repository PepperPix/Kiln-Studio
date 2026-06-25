namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public class RecentProjectsStoreTests
{
    private const string SiteAlpha = "Alpha Site";
    private const string SiteBeta = "Beta Site";
    private const string SiteGamma = "Gamma Site";
    private const int ThreeProjects = 3;
    private const int TwoProjects = 2;
    private const int FifteenProjects = 15;
    private const int MaxCap = 10;

    private static string MakeTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Test]
    public async Task GetAll_EmptyStore_ReturnsEmptyList()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new RecentProjectsStore(dir);
            await Assert.That(store.GetAll().Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Add_ThreeProjects_ReturnsMostRecentFirst()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new RecentProjectsStore(dir);
            store.Add("/projects/alpha", SiteAlpha);
            store.Add("/projects/beta", SiteBeta);
            store.Add("/projects/gamma", SiteGamma);

            var all = store.GetAll();

            await Assert.That(all.Count).IsEqualTo(ThreeProjects);
            await Assert.That(all[0].Name).IsEqualTo(SiteGamma);
            await Assert.That(all[1].Name).IsEqualTo(SiteBeta);
            await Assert.That(all[^1].Name).IsEqualTo(SiteAlpha);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Add_DuplicatePath_DeduplicatesAndMovesToFront()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new RecentProjectsStore(dir);
            store.Add("/projects/alpha", SiteAlpha);
            store.Add("/projects/beta", SiteBeta);
            store.Add("/projects/alpha", SiteAlpha);

            var all = store.GetAll();

            await Assert.That(all.Count).IsEqualTo(TwoProjects);
            await Assert.That(all[0].Path).IsEqualTo("/projects/alpha");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Add_DuplicatePath_CaseInsensitive_Deduplicates()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new RecentProjectsStore(dir);
            store.Add("/projects/Alpha", SiteAlpha);
            store.Add("/projects/alpha", SiteAlpha);

            var all = store.GetAll();

            await Assert.That(all.Count).IsEqualTo(1);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Add_MoreThanTenProjects_CapsAtTen()
    {
        var dir = MakeTempDir();
        try
        {
            var store = new RecentProjectsStore(dir);
            for (var i = 0; i < FifteenProjects; i++)
                store.Add($"/projects/site-{i}", $"Site {i}");

            await Assert.That(store.GetAll().Count).IsEqualTo(MaxCap);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task GetAll_CorruptJsonFile_ReturnsEmptyList()
    {
        var dir = MakeTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "recent.json"), "{ not valid json }}}");
            var store = new RecentProjectsStore(dir);

            await Assert.That(store.GetAll().Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task GetAll_RoundTrip_PersistsAcrossInstances()
    {
        var dir = MakeTempDir();
        try
        {
            var store1 = new RecentProjectsStore(dir);
            store1.Add("/projects/alpha", SiteAlpha);
            store1.Add("/projects/beta", SiteBeta);

            var store2 = new RecentProjectsStore(dir);
            var all = store2.GetAll();

            await Assert.That(all.Count).IsEqualTo(TwoProjects);
            await Assert.That(all[0].Name).IsEqualTo(SiteBeta);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
