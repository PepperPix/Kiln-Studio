namespace Kiln.Studio.Tests;

using Kiln.Models;
using Kiln.Services;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Microsoft.Extensions.DependencyInjection;

public class ContentServiceLoadSaveTests
{
    private const string PostsCollection = "posts";
    private const string PostsDirectory = "content/posts";

    [Test]
    public async Task Load_SplitsFrontmatterAndBody()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath,
                "---\ntitle: Hello World\ndate: 2026-06-25\n---\n\nMarkdown body here.");

            var service = new ContentService();
            var doc = service.Load(filePath);

            await Assert.That(doc.FrontMatter).Contains("title: Hello World");
            await Assert.That(doc.Body).Contains("Markdown body here.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task Load_NoFrontmatter_ReturnsEmptyFrontmatterAndFullBody()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "test.md");
            await File.WriteAllTextAsync(filePath, "Just body content.");

            var service = new ContentService();
            var doc = service.Load(filePath);

            await Assert.That(doc.FrontMatter).IsEqualTo("");
            await Assert.That(doc.Body).Contains("Just body content.");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task RoundTrip_LoadThenSave_EngineCanStillReadTitle()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempDir);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("round-trip-site", tempDir);
            var projectPath = scaffoldResult.ProjectPath;

            var postsDir = Path.Combine(projectPath, PostsDirectory);
            var mdFile = Directory.GetFiles(postsDir, "*.md", SearchOption.TopDirectoryOnly)
                .First();

            var service = new ContentService();
            var doc = service.Load(mdFile);
            var originalTitle = doc.FrontMatter;

            service.Save(mdFile, doc.FrontMatter, doc.Body);

            using var readProvider = host.CreateProvider(projectPath);
            var siteConfig = readProvider.GetRequiredService<ISiteConfigLoader>().Load(projectPath);
            var postsGroup = siteConfig.Collections[PostsCollection];
            var reader = readProvider.GetRequiredService<IContentReader>();
            var item = reader.ReadSingleFile(mdFile, postsGroup);

            await Assert.That(item.Title).IsNotNull();
            await Assert.That(item.Title).IsNotEqualTo("");
            await Assert.That(doc.FrontMatter).IsEqualTo(originalTitle);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}

public class ContentServiceCreatePageTests
{
    private const string PageTitle = "My First Post";
    private const string ExpectedSlug = "my-first-post";
    private const string DuplicateSlug = "my-first-post-2";

    [Test]
    public async Task CreatePage_CreatesFileWithCorrectSlug()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            var path = service.CreatePage(tempDir, PageTitle);

            await Assert.That(Path.GetFileName(path)).IsEqualTo($"{ExpectedSlug}.md");
            await Assert.That(File.Exists(path)).IsTrue();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_FileContainsTitleAndDraftTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            var path = service.CreatePage(tempDir, PageTitle);
            var content = await File.ReadAllTextAsync(path);

            await Assert.That(content).Contains($"title: {PageTitle}");
            await Assert.That(content).Contains("draft: true");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_Collision_AddsSuffix()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            service.CreatePage(tempDir, PageTitle);
            var second = service.CreatePage(tempDir, PageTitle);

            await Assert.That(Path.GetFileName(second)).IsEqualTo($"{DuplicateSlug}.md");
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_EmptyTitle_ThrowsArgumentException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new ContentService();
            await Assert.That(() => service.CreatePage(tempDir, "")).Throws<ArgumentException>();
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task CreatePage_CreatedFileReadableByEngine()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempDir);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("new-page-site", tempDir);
            var projectPath = scaffoldResult.ProjectPath;
            var postsDir = Path.Combine(projectPath, "content/posts");

            var service = new ContentService();
            var newFilePath = service.CreatePage(postsDir, PageTitle);

            using var readProvider = host.CreateProvider(projectPath);
            var siteConfig = readProvider.GetRequiredService<ISiteConfigLoader>().Load(projectPath);
            var postsGroup = siteConfig.Collections["posts"];
            var reader = readProvider.GetRequiredService<IContentReader>();
            var item = reader.ReadSingleFile(newFilePath, postsGroup);

            await Assert.That(item.Title).IsEqualTo(PageTitle);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}

public class EditorViewModelTests
{
    private const string TestTitle = "Test Post";
    private const string InitialBody = "Hello world!";
    private const string ModifiedBody = "Updated body.";
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
}

public class ShellViewModelEditorTests
{
    private const string NewPostTitle = "My New Post";
    private const int TwoTaxonomyFields = 2;

    [Test]
    public async Task NewPageAsync_CreatesFileAndOpensInEditor()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempParent);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("editor-test-site", tempParent);
            var projectPath = scaffoldResult.ProjectPath;

            var explorer = new ProjectExplorerViewModel();
            var editor = new EditorViewModel(new ContentService());
            var store = new RecentProjectsStore(storeDir);
#pragma warning disable S107
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(projectPath),
                new NullInputDialog(),
                store,
                new ContentService(),
                new FixedNewPageDialog("posts", NewPostTitle),
                explorer,
                editor,
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());
#pragma warning restore S107

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await vm.NewPageCommand.ExecuteAsync(null);

            await Assert.That(editor.HasDocument).IsTrue();
            await Assert.That(editor.FilePath).IsNotNull();
            await Assert.That(editor.Title).IsEqualTo(NewPostTitle);

            var postsCollection = explorer.Collections.FirstOrDefault(c => c.Name == "posts");
            await Assert.That(postsCollection).IsNotNull();
            await Assert.That(postsCollection!.FilteredEntries.Count).IsGreaterThan(0);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task NewPageAsync_NullDialog_NoChange()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempParent);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("no-page-site", tempParent);
            var projectPath = scaffoldResult.ProjectPath;

            var editor = new EditorViewModel(new ContentService());
#pragma warning disable S107
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(projectPath),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                editor,
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());
#pragma warning restore S107

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await vm.NewPageCommand.ExecuteAsync(null);

            await Assert.That(editor.HasDocument).IsFalse();
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task SelectionChange_LoadsEditorWithSelectedEntry()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempParent);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("selection-test-site", tempParent);
            var projectPath = scaffoldResult.ProjectPath;

            var explorer = new ProjectExplorerViewModel();
            var editor = new EditorViewModel(new ContentService());
#pragma warning disable S107
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(projectPath),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                explorer,
                editor,
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());
#pragma warning restore S107

            await vm.OpenProjectCommand.ExecuteAsync(null);

            var postsCollection = explorer.Collections.FirstOrDefault(c => c.Name == "posts");
            await Assert.That(postsCollection).IsNotNull();
            var firstEntry = postsCollection!.FilteredEntries[0];

            explorer.SelectedEntry = firstEntry;

            await Assert.That(editor.HasDocument).IsTrue();
            await Assert.That(editor.FilePath).IsEqualTo(firstEntry.SourcePath);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task SelectionChange_PopulatesTaxonomyFieldsWithCrossItemAutocompleteSuggestions()
    {
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var host = new EngineHost();
            using var scaffoldProvider = host.CreateProvider(tempParent);
            var scaffolder = scaffoldProvider.GetRequiredService<IScaffolder>();
            var scaffoldResult = scaffolder.CreateSite("taxonomy-test-site", tempParent);
            var projectPath = scaffoldResult.ProjectPath;

            var explorer = new ProjectExplorerViewModel();
            var editor = new EditorViewModel(new ContentService(), new ContentFrontmatterWriter(), new TaxonomyValueCache());
#pragma warning disable S107
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(projectPath),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                explorer,
                editor,
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new FakeBuildService(),
                new FakeDeploymentService(),
                new NullSettingsDialog(), new NullDeploymentConfigStore(), new NullPublishService(), new FakeContentFrontmatterWriter());
#pragma warning restore S107

            await vm.OpenProjectCommand.ExecuteAsync(null);

            var postsCollection = explorer.Collections.FirstOrDefault(c => c.Name == "posts");
            await Assert.That(postsCollection).IsNotNull();

            // Every demo post has its own "tags"/"categories" values, but the taxonomy value
            // cache built at project-open time aggregates values from ALL posts. Selecting any
            // single post should therefore see MORE suggestions than its own tag count — proving
            // autocomplete surfaces values used elsewhere in the project, not just the current item.
            var entry = postsCollection!.FilteredEntries[0];
            explorer.SelectedEntry = entry;

            await Assert.That(editor.TaxonomyFields.Count).IsEqualTo(TwoTaxonomyFields);
            var tagsField = editor.TaxonomyFields.Single(f => f.Name == "tags");

            await Assert.That(tagsField.Suggestions.Count).IsGreaterThan(tagsField.Values.Count);
            foreach (var ownValue in tagsField.Values)
                await Assert.That(tagsField.Suggestions).Contains(ownValue);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }
}

