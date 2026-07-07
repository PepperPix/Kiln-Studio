namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public sealed class TaxonomyValueCacheTests
{
    private const int TwoValues = 2;

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Test]
    public async Task GetSuggestions_NoCacheFile_ReturnsEmpty()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new TaxonomyValueCache();
            var values = cache.GetSuggestions(dir, "tags");

            await Assert.That(values.Count).IsEqualTo(0);
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Rebuild_ThenGetSuggestions_ReturnsValues()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new TaxonomyValueCache();
            cache.Rebuild(dir, new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["tags"] = new[] { "dotnet", "kiln" },
                ["categories"] = new[] { "news" },
            });

            var tags = cache.GetSuggestions(dir, "tags");
            var categories = cache.GetSuggestions(dir, "categories");

            await Assert.That(tags.Count).IsEqualTo(TwoValues);
            await Assert.That(tags).Contains("dotnet");
            await Assert.That(tags).Contains("kiln");
            await Assert.That(categories).Contains("news");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task Rebuild_CreatesDotKilnDirectory()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new TaxonomyValueCache();
            cache.Rebuild(dir, new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["tags"] = new[] { "a" },
            });

            await Assert.That(File.Exists(Path.Combine(dir, ".kiln", "taxonomy-values.json"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task AddValues_MergesWithExisting_NeverRemoves()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new TaxonomyValueCache();
            cache.Rebuild(dir, new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["tags"] = new[] { "dotnet" },
            });

            cache.AddValues(dir, "tags", ["kiln", "dotnet"]);

            var values = cache.GetSuggestions(dir, "tags");

            await Assert.That(values.Count).IsEqualTo(TwoValues);
            await Assert.That(values).Contains("dotnet");
            await Assert.That(values).Contains("kiln");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task AddValues_EmptyList_DoesNotCreateFile()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new TaxonomyValueCache();
            cache.AddValues(dir, "tags", []);

            await Assert.That(File.Exists(Path.Combine(dir, ".kiln", "taxonomy-values.json"))).IsFalse();
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public async Task AddValues_NoPriorCache_CreatesNewEntry()
    {
        var dir = CreateTempDir();
        try
        {
            var cache = new TaxonomyValueCache();
            cache.AddValues(dir, "categories", ["news"]);

            var values = cache.GetSuggestions(dir, "categories");

            await Assert.That(values.Count).IsEqualTo(1);
            await Assert.That(values).Contains("news");
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
