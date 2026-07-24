namespace Kiln.Studio.Tests;

using Services;
using TestSupport;
using ViewModels;

public class EditorViewModelTests
{
    private const string TestTitle = "Test Post";
    private const string InitialBody = "Hello world!";
    private const string ModifiedBody = "Updated body.";
    private const int TestImageFileSizeBytes = 300 * 1024;
    private const int TwoTaxonomyFields = 2;
    private const int ExpectedDateYear = 2026;
    private const int ExpectedDateMonth = 7;
    private const int ExpectedDateDay = 9;

    [Test]
    public async Task Load_SetsPropertiesAndClearsDirty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var vm = new EditorViewModel(new ContentService());
            vm.Load(filePath);

            await Assert.That(vm.HasDocument).IsTrue();
            await Assert.That(vm.IsDirty).IsFalse();
            await Assert.That(vm.FilePath).IsEqualTo(filePath);
            await Assert.That(vm.Title).IsEqualTo(TestTitle);
            await Assert.That(vm.FrontMatter).DoesNotContain("title");
            await Assert.That(vm.Body).Contains(InitialBody);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Load_PopulatesScalarFields_AndStripsOwnedKeysFromFrontMatter()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                $"---\ntitle: {TestTitle}\ndate: 2026-07-09\ndescription: A short summary\n" +
                "tags:\n  - dotnet\ncategories:\n  - news\nid: abc123\ndraft: false\n" +
                $"---\n\n{InitialBody}");

            var vm = new EditorViewModel(new ContentService(), new ContentFrontmatterWriter(), new FakeTaxonomyValueCache());
            vm.Load(filePath, tempDir, ["tags", "categories"]);

            await Assert.That(vm.Title).IsEqualTo(TestTitle);
            await Assert.That(vm.Date).IsNotNull();
            await Assert.That(vm.Date!.Value.Year).IsEqualTo(ExpectedDateYear);
            await Assert.That(vm.Date!.Value.Month).IsEqualTo(ExpectedDateMonth);
            await Assert.That(vm.Date!.Value.Day).IsEqualTo(ExpectedDateDay);
            await Assert.That(vm.Description).IsEqualTo("A short summary");

            await Assert.That(vm.FrontMatter).DoesNotContain("title");
            await Assert.That(vm.FrontMatter).DoesNotContain("date");
            await Assert.That(vm.FrontMatter).DoesNotContain("description");
            await Assert.That(vm.FrontMatter).DoesNotContain("tags");
            await Assert.That(vm.FrontMatter).DoesNotContain("categories");
            await Assert.That(vm.FrontMatter).Contains("id: abc123");
            await Assert.That(vm.FrontMatter).Contains("draft: false");
            await Assert.That(vm.IsDirty).IsFalse();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task SaveAsync_WritesScalarFields_ToFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var frontmatterWriter = new ContentFrontmatterWriter();
            var vm = new EditorViewModel(new ContentService(), frontmatterWriter, new FakeTaxonomyValueCache());
            vm.Load(filePath, tempDir, ["tags"]);

            vm.Title = "Updated Title";
            vm.Date = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);
            vm.Description = "Updated description";
            vm.TaxonomyFields.Single(f => f.Name == "tags").Values.Add("kiln");

            await vm.SaveCommand.ExecuteAsync(null);

            var written = await File.ReadAllTextAsync(filePath);
            await Assert.That(written).Contains("title: Updated Title");
            await Assert.That(written).Contains("date: 2026-03-15");
            await Assert.That(written).Contains("description: Updated description");
            await Assert.That(written).Contains("kiln");

            await Assert.That(frontmatterWriter.GetScalarValue(filePath, "title")).IsEqualTo("Updated Title");
            await Assert.That(frontmatterWriter.GetScalarValue(filePath, "date")).IsEqualTo("2026-03-15");
            await Assert.That(frontmatterWriter.GetScalarValue(filePath, "description")).IsEqualTo("Updated description");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task TitleChange_MarksDocumentDirtyAndEnablesSave()
    {
        // Regression test analogous to AddTaxonomyValue_MarksDocumentDirtyAndEnablesSave: structured
        // scalar fields must mark the document dirty too, otherwise SaveCommand.CanExecute stays
        // false and the change is silently lost.
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var vm = new EditorViewModel(new ContentService());
            vm.Load(filePath);

            await Assert.That(vm.IsDirty).IsFalse();
            await Assert.That(vm.SaveCommand.CanExecute(null)).IsFalse();

            vm.Title = "Neu";

            await Assert.That(vm.IsDirty).IsTrue();
            await Assert.That(vm.SaveCommand.CanExecute(null)).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task BodyChange_SetsDirtyAndEnablesSave()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var vm = new EditorViewModel(new ContentService());
            vm.Load(filePath);
            vm.Body = ModifiedBody;

            await Assert.That(vm.IsDirty).IsTrue();
            await Assert.That(vm.SaveCommand.CanExecute(null)).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task SaveAsync_WritesFileToDisk_ClearsDirty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var vm = new EditorViewModel(new ContentService());
            vm.Load(filePath);
            vm.Body = ModifiedBody;

            await vm.SaveCommand.ExecuteAsync(null);

            await Assert.That(vm.IsDirty).IsFalse();
            var written = await File.ReadAllTextAsync(filePath);
            await Assert.That(written).Contains(ModifiedBody);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Load_WithTaxonomyNames_PopulatesFieldsFromWriterAndCache()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                $"---\ntitle: {TestTitle}\ntags:\n  - dotnet\n---\n\n{InitialBody}");

            var frontmatterWriter = new ContentFrontmatterWriter();
            var cache = new FakeTaxonomyValueCache();
            cache.SuggestionsByTaxonomy["tags"] = ["dotnet", "kiln"];

            var vm = new EditorViewModel(new ContentService(), frontmatterWriter, cache);
            vm.Load(filePath, tempDir, ["tags", "categories"]);

            await Assert.That(vm.TaxonomyFields.Count).IsEqualTo(TwoTaxonomyFields);

            var tagsField = vm.TaxonomyFields.Single(f => f.Name == "tags");
            await Assert.That(tagsField.Values).Contains("dotnet");
            await Assert.That(tagsField.Suggestions).Contains("kiln");

            var categoriesField = vm.TaxonomyFields.Single(f => f.Name == "categories");
            await Assert.That(categoriesField.Values.Count).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task SaveAsync_WritesTaxonomyValues_ToFileAndCache()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var frontmatterWriter = new ContentFrontmatterWriter();
            var cache = new FakeTaxonomyValueCache();

            var vm = new EditorViewModel(new ContentService(), frontmatterWriter, cache);
            vm.Load(filePath, tempDir, ["tags"]);
            vm.TaxonomyFields.Single(f => f.Name == "tags").Values.Add("newtag");

            await vm.SaveCommand.ExecuteAsync(null);

            var written = await File.ReadAllTextAsync(filePath);
            await Assert.That(written).Contains("newtag");
            await Assert.That(written).Contains(TestTitle);

            var persisted = frontmatterWriter.GetTaxonomyValues(filePath, "tags");
            await Assert.That(persisted).Contains("newtag");

            await Assert.That(cache.AddValuesCalls.Count).IsEqualTo(1);
            await Assert.That(cache.AddValuesCalls[0].TaxonomyName).IsEqualTo("tags");
            await Assert.That(cache.AddValuesCalls[0].Values).Contains("newtag");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task RemoveTaxonomyValue_ThenSave_RemovesItFromFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                $"---\ntitle: {TestTitle}\ntags:\n  - keep\n  - remove-me\n---\n\n{InitialBody}");

            var frontmatterWriter = new ContentFrontmatterWriter();
            var vm = new EditorViewModel(new ContentService(), frontmatterWriter, new FakeTaxonomyValueCache());
            vm.Load(filePath, tempDir, ["tags"]);

            var tagsField = vm.TaxonomyFields.Single(f => f.Name == "tags");
            tagsField.Values.Remove("remove-me");

            await vm.SaveCommand.ExecuteAsync(null);

            var persisted = frontmatterWriter.GetTaxonomyValues(filePath, "tags");
            await Assert.That(persisted).Contains("keep");
            await Assert.That(persisted).DoesNotContain("remove-me");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task AddTaxonomyValue_MarksDocumentDirtyAndEnablesSave()
    {
        // Regression test: adding/removing a chip via a TaxonomyFieldViewModel must mark the
        // document dirty, otherwise SaveCommand.CanExecute stays false and the change is silently
        // lost (real bug found 2026-07-09 — chip edits never touched IsDirty).
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var vm = new EditorViewModel(new ContentService(), new ContentFrontmatterWriter(), new FakeTaxonomyValueCache());
            vm.Load(filePath, tempDir, ["tags"]);

            await Assert.That(vm.IsDirty).IsFalse();
            await Assert.That(vm.SaveCommand.CanExecute(null)).IsFalse();

            vm.TaxonomyFields.Single(f => f.Name == "tags").Values.Add("newtag");

            await Assert.That(vm.IsDirty).IsTrue();
            await Assert.That(vm.SaveCommand.CanExecute(null)).IsTrue();

            vm.TaxonomyFields.Single(f => f.Name == "tags").Values.Remove("newtag");

            await vm.SaveCommand.ExecuteAsync(null);
            await Assert.That(vm.IsDirty).IsFalse();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_NullResult_ReturnsNull()
    {
        var vm = new EditorViewModel(new ContentService());

        var snippet = await vm.PrepareAssetSnippetAsync(null);

        await Assert.That(snippet).IsNull();
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_LibraryDestination_ImageFile_ReturnsImageMarkdownWithAssetsPath()
    {
        var vm = new EditorViewModel(new ContentService());

        var snippet = await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.Library, "images/photo.png"));

        await Assert.That(snippet).IsEqualTo("![](/assets/images/photo.png)");
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_LibraryDestination_NonImageFile_ReturnsLinkMarkdown()
    {
        var vm = new EditorViewModel(new ContentService());

        var snippet = await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.Library, "downloads/handbuch.pdf"));

        await Assert.That(snippet).IsEqualTo("[handbuch.pdf](/assets/downloads/handbuch.pdf)");
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_PageBundleDestination_ReturnsRelativeMarkdown()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var pageBundleService = new FakePageBundleService
            {
                UploadAssetResult = new PageBundleUploadResult(filePath, "photo.png", WasConverted: false)
            };

            var vm = new EditorViewModel(new ContentService(), pageBundleService: pageBundleService);
            vm.Load(filePath);

            var snippet = await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.PageBundle, "/tmp/some/photo.png"));

            await Assert.That(snippet).IsEqualTo("![](./photo.png)");
            await Assert.That(pageBundleService.LastSourcePath).IsEqualTo(filePath);
            await Assert.That(pageBundleService.LastUploadedFilePath).IsEqualTo("/tmp/some/photo.png");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_PageBundleDestination_WasConverted_InvokesConvertedHandler()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var newSourcePath = Path.Combine(tempDir, "test", "index.md");
            var pageBundleService = new FakePageBundleService
            {
                UploadAssetResult = new PageBundleUploadResult(newSourcePath, "handbuch.pdf", WasConverted: true)
            };

            var vm = new EditorViewModel(new ContentService(), pageBundleService: pageBundleService);
            vm.Load(filePath);

            string? handlerArg = null;
            var handlerCalled = 0;
            vm.SetPageBundleConvertedHandler(path =>
            {
                handlerArg = path;
                handlerCalled++;
                return Task.CompletedTask;
            });

            var snippet = await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.PageBundle, "/tmp/some/handbuch.pdf"));

            await Assert.That(snippet).IsEqualTo("[handbuch.pdf](./handbuch.pdf)");
            await Assert.That(handlerCalled).IsEqualTo(1);
            await Assert.That(handlerArg).IsEqualTo(newSourcePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_PageBundleDestination_NotConverted_DoesNotInvokeConvertedHandler()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var pageBundleService = new FakePageBundleService
            {
                UploadAssetResult = new PageBundleUploadResult(filePath, "photo.png", WasConverted: false)
            };

            var vm = new EditorViewModel(new ContentService(), pageBundleService: pageBundleService);
            vm.Load(filePath);

            var handlerCalled = 0;
            vm.SetPageBundleConvertedHandler(_ =>
            {
                handlerCalled++;
                return Task.CompletedTask;
            });

            await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.PageBundle, "/tmp/some/photo.png"));

            await Assert.That(handlerCalled).IsEqualTo(0);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_PageBundleDestination_DirtyDocument_SavesBeforeUpload()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");

            var pageBundleService = new FakePageBundleService
            {
                UploadAssetResult = new PageBundleUploadResult(filePath, "photo.png", WasConverted: false)
            };

            var vm = new EditorViewModel(new ContentService(), pageBundleService: pageBundleService);
            vm.Load(filePath);
            vm.Body = ModifiedBody;
            await Assert.That(vm.IsDirty).IsTrue();

            await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.PageBundle, "/tmp/some/photo.png"));

            await Assert.That(vm.IsDirty).IsFalse();
            var written = await File.ReadAllTextAsync(filePath);
            await Assert.That(written).Contains(ModifiedBody);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_ImageWithinMaxWidth_SetsOriginalSizeFeedback()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "site.yaml"), "title: Test Site\nbaseUrl: http://localhost:5000\n");
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");
            var staticDir = Path.Combine(tempDir, "static", "images");
            Directory.CreateDirectory(staticDir);
            var imagePath = Path.Combine(staticDir, "photo.png");
            await File.WriteAllBytesAsync(imagePath, new byte[TestImageFileSizeBytes]);

            var reader = new FakeImageDimensionReader((800, 600));
            var vm = new EditorViewModel(new ContentService(), imageDimensionReader: reader);
            vm.Load(filePath, tempDir);

            await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.Library, "images/photo.png"));

            await Assert.That(vm.LastAssetFeedback)
                .IsEqualTo("800×600px, 300 KB — bleibt beim Build in Originalgröße (Limit: 2000px).");
            await Assert.That(reader.LastFilePath).IsEqualTo(imagePath);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_ImageOptimizationDisabled_SetsDisabledFeedback()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "site.yaml"),
                "title: Test Site\nbaseUrl: http://localhost:5000\nimages:\n  enabled: false\n");
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");
            var staticDir = Path.Combine(tempDir, "static", "images");
            Directory.CreateDirectory(staticDir);
            var imagePath = Path.Combine(staticDir, "photo.png");
            await File.WriteAllBytesAsync(imagePath, new byte[TestImageFileSizeBytes]);

            var reader = new FakeImageDimensionReader((800, 600));
            var vm = new EditorViewModel(new ContentService(), imageDimensionReader: reader);
            vm.Load(filePath, tempDir);

            await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.Library, "images/photo.png"));

            await Assert.That(vm.LastAssetFeedback)
                .IsEqualTo("800×600px, 300 KB — Bild-Optimierung ist für dieses Projekt deaktiviert.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_ImageExceedsMaxWidth_SetsScaledFeedback()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(tempDir, "site.yaml"),
                "title: Test Site\nbaseUrl: http://localhost:5000\nimages:\n  maxWidth: 400\n");
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");
            var staticDir = Path.Combine(tempDir, "static", "images");
            Directory.CreateDirectory(staticDir);
            var imagePath = Path.Combine(staticDir, "photo.png");
            await File.WriteAllBytesAsync(imagePath, new byte[TestImageFileSizeBytes]);

            var reader = new FakeImageDimensionReader((800, 600));
            var vm = new EditorViewModel(new ContentService(), imageDimensionReader: reader);
            vm.Load(filePath, tempDir);

            await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.Library, "images/photo.png"));

            await Assert.That(vm.LastAssetFeedback)
                .IsEqualTo("800×600px, 300 KB — wird beim Build auf 400px Breite skaliert.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_DimensionsUnreadable_LeavesFeedbackNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "site.yaml"), "title: Test Site\nbaseUrl: http://localhost:5000\n");
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{InitialBody}");
            var staticDir = Path.Combine(tempDir, "static", "images");
            Directory.CreateDirectory(staticDir);
            var imagePath = Path.Combine(staticDir, "photo.png");
            await File.WriteAllBytesAsync(imagePath, new byte[TestImageFileSizeBytes]);

            var reader = new FakeImageDimensionReader(null);
            var vm = new EditorViewModel(new ContentService(), imageDimensionReader: reader);
            vm.Load(filePath, tempDir);

            await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.Library, "images/photo.png"));

            await Assert.That(vm.LastAssetFeedback).IsNull();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PrepareAssetSnippetAsync_NonImageFile_LeavesFeedbackNull()
    {
        var reader = new FakeImageDimensionReader((800, 600));
        var vm = new EditorViewModel(new ContentService(), imageDimensionReader: reader);

        await vm.PrepareAssetSnippetAsync(new AssetPickerResult(AssetPickerDestination.Library, "downloads/handbuch.pdf"));

        await Assert.That(vm.LastAssetFeedback).IsNull();
    }

    [Test]
    public async Task PreviewMarkdown_RewritesPageBundleRelativeImage_ToAbsoluteFileUri()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var bundleDir = Path.Combine(tempDir, "my-post");
        Directory.CreateDirectory(bundleDir);
        try
        {
            var imagePath = Path.Combine(bundleDir, "photo.png");
            await File.WriteAllTextAsync(imagePath, "fake-png");
            var filePath = Path.Combine(bundleDir, "index.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n![alt](./photo.png)");

            var vm = new EditorViewModel(new ContentService());
            vm.Load(filePath);

            var expectedUri = new Uri(imagePath).AbsoluteUri;
            await Assert.That(vm.PreviewMarkdown).Contains($"![alt]({expectedUri})");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PreviewMarkdown_RewritesLibraryAbsoluteImage_ToAbsoluteFileUriUnderStatic()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var contentDir = Path.Combine(tempDir, "content");
        Directory.CreateDirectory(contentDir);
        var staticDir = Path.Combine(tempDir, "static", "images");
        Directory.CreateDirectory(staticDir);
        try
        {
            var imagePath = Path.Combine(staticDir, "photo.png");
            await File.WriteAllTextAsync(imagePath, "fake-png");
            var filePath = Path.Combine(contentDir, "post.md");
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n![alt](/assets/images/photo.png)");

            var vm = new EditorViewModel(new ContentService());
            vm.Load(filePath, tempDir);

            var expectedUri = new Uri(imagePath).AbsoluteUri;
            await Assert.That(vm.PreviewMarkdown).Contains($"![alt]({expectedUri})");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task PreviewMarkdown_LeavesUnresolvableOrExternalImages_Untouched()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "post.md");
            const string externalImage = "![alt](https://example.com/photo.png)";
            const string missingImage = "![alt](./does-not-exist.png)";
            await File.WriteAllTextAsync(filePath, $"---\ntitle: {TestTitle}\n---\n\n{externalImage}\n\n{missingImage}");

            var vm = new EditorViewModel(new ContentService());
            vm.Load(filePath, tempDir);

            await Assert.That(vm.PreviewMarkdown).Contains(externalImage);
            await Assert.That(vm.PreviewMarkdown).Contains(missingImage);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
