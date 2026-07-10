namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;

public class ShellViewModelAssetManagementTests
{
    [Test]
    public async Task HandlePageBundleConvertedAsync_RefreshesExplorer_AndReselectsEntryAtNewPath()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        var uploadSourceDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(uploadSourceDir);
        try
        {
            var uploadedFile = Path.Combine(uploadSourceDir, "photo.png");
            await File.WriteAllTextAsync(uploadedFile, "fake-png");

            var dialog = new FakeAssetPickerDialog(new AssetPickerResult(AssetPickerDestination.PageBundle, uploadedFile));
            var editor = new EditorViewModel(new ContentService(), assetPickerDialog: dialog, pageBundleService: new PageBundleService());

            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("my-site"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                editor,
                new NullPreviewServer(),
                new NullBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService(),
                new NullSettingsDialog(),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            // Pick a flat (non-page-bundle) entry — the scaffolded default theme also ships one
            // tutorial post that is already a page bundle (index.md in its own folder).
            var flatEntry = vm.Explorer.Collections
                .SelectMany(c => c.FilteredEntries)
                .First(e => !e.SourcePath.EndsWith("index.md", StringComparison.Ordinal));
            var oldSourcePath = flatEntry.SourcePath;

            vm.Explorer.SelectedEntry = flatEntry;
            await Assert.That(vm.Editor.FilePath).IsEqualTo(oldSourcePath);

            var slug = Path.GetFileNameWithoutExtension(oldSourcePath);
            var expectedNewSourcePath = Path.Combine(Path.GetDirectoryName(oldSourcePath)!, slug, "index.md");

            var snippet = await vm.Editor.PickAndPrepareAssetAsync();

            await Assert.That(snippet).IsEqualTo("![](./photo.png)");
            await Assert.That(File.Exists(oldSourcePath)).IsFalse();
            await Assert.That(File.Exists(expectedNewSourcePath)).IsTrue();
            await Assert.That(File.Exists(Path.Combine(Path.GetDirectoryName(expectedNewSourcePath)!, "photo.png"))).IsTrue();

            // Explorer was refreshed and the item at the new path re-selected — this is what
            // re-triggers the existing OnExplorerPropertyChanged reload path. Compared via
            // Path.GetFullPath (canonicalizes separators) rather than raw string equality: on
            // Windows, a collection's raw SourcePath can retain forward slashes inherited from
            // site.yaml that plain Path.Combine does not normalize away, while expectedNewSourcePath
            // (built purely via Path.GetDirectoryName/Path.Combine) is always fully normalized —
            // the two can be the same file on disk without being byte-identical strings.
            await Assert.That(vm.Explorer.SelectedEntry).IsNotNull();
            await Assert.That(Path.GetFullPath(vm.Explorer.SelectedEntry!.SourcePath)).IsEqualTo(Path.GetFullPath(expectedNewSourcePath));
            await Assert.That(Path.GetFullPath(vm.Editor.FilePath!)).IsEqualTo(Path.GetFullPath(expectedNewSourcePath));
            await Assert.That(vm.Editor.HasDocument).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
            if (Directory.Exists(uploadSourceDir)) Directory.Delete(uploadSourceDir, recursive: true);
        }
    }
}
