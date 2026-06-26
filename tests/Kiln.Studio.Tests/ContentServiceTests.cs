namespace Kiln.Studio.Tests;

using Kiln.Models;
using Kiln.Services;
using Kiln.Studio.Services;
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
            await Assert.That(vm.FrontMatter).Contains(TestTitle);
            await Assert.That(vm.Body).Contains(InitialBody);
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
}

public class ShellViewModelEditorTests
{
    private const string NewPostTitle = "My New Post";

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
                new FakeDeploymentService());
#pragma warning restore S107

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await vm.NewPageCommand.ExecuteAsync(null);

            await Assert.That(editor.HasDocument).IsTrue();
            await Assert.That(editor.FilePath).IsNotNull();
            await Assert.That(editor.FrontMatter).Contains(NewPostTitle);

            var postsCollection = explorer.Collections.FirstOrDefault(c => c.Name == "posts");
            await Assert.That(postsCollection).IsNotNull();
            await Assert.That(postsCollection!.Entries.Count).IsGreaterThan(0);
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
                new FakeDeploymentService());
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
                new FakeDeploymentService());
#pragma warning restore S107

            await vm.OpenProjectCommand.ExecuteAsync(null);

            var postsCollection = explorer.Collections.FirstOrDefault(c => c.Name == "posts");
            await Assert.That(postsCollection).IsNotNull();
            var firstEntry = postsCollection!.Entries[0];

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
}

file sealed class NullFolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}

file sealed class FixedFolderPicker(string path) : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(path);
}

file sealed class NullInputDialog : IInputDialog
{
    public Task<string?> PromptAsync(string title, string message) => Task.FromResult<string?>(null);
}

file sealed class NullNewPageDialog : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames) => Task.FromResult<NewPageRequest?>(null);
}

file sealed class FixedNewPageDialog(string collectionName, string title) : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames)
        => Task.FromResult<NewPageRequest?>(new NewPageRequest(collectionName, title));
}
