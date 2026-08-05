namespace Kiln.Studio.Tests;

using Models;
using Kiln.Services;
using Services;
using TestSupport;
using ViewModels;
using Microsoft.Extensions.DependencyInjection;

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
#pragma warning disable S107 // The test intentionally wires the full shell constructor to exercise the real composition path.
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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));
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
#pragma warning disable S107 // The test intentionally wires the full shell constructor to exercise the real composition path.
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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));
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
#pragma warning disable S107 // The test intentionally wires the full shell constructor to exercise the real composition path.
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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));
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
#pragma warning disable S107 // The test intentionally wires the full shell constructor to exercise the real composition path.
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
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter(),
                MenuEditorTestFactory.CreateDummy(),
                new ThemeManagerViewModel(
                new ThemeService(new SiteSettingsService(new EngineHost())),
                new NullFilePicker(),
                new NullInputDialog()));
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
