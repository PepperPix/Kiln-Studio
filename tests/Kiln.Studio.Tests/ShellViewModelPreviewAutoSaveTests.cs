namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;

public class ShellViewModelPreviewAutoSaveTests
{
    private const int AutoSaveSettleDelayMs = 2000;

    [Test]
    public async Task StartFullPreview_WhenDirty_SavesBeforeStartingServer()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        try
        {
            var editor = new EditorViewModel(new ContentService());
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("my-site"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                editor,
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);
            var post = vm.Explorer.Collections.First(c => c.Name == "posts").FilteredEntries.First();
            vm.Explorer.SelectedEntry = post;
            await Assert.That(vm.Editor.HasDocument).IsTrue();

            const string newBody = "Edited before preview.";
            vm.Editor.Body = newBody;
            await Assert.That(vm.Editor.IsDirty).IsTrue();

            await vm.StartFullPreviewCommand.ExecuteAsync(null);

            await Assert.That(vm.Editor.IsDirty).IsFalse();
            var written = await File.ReadAllTextAsync(vm.Editor.FilePath!);
            await Assert.That(written).Contains(newBody);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    [Test]
    public async Task WhilePreviewIsServing_EditingBody_AutoSavesAfterDebounce()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        var tempParent = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempParent);
        try
        {
            var editor = new EditorViewModel(new ContentService());
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new FixedFolderPicker(tempParent),
                new FixedInputDialog("my-site"),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                editor,
                new FakePreviewServer(),
                new FakeBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter());

            await vm.NewSiteCommand.ExecuteAsync(null);
            var post = vm.Explorer.Collections.First(c => c.Name == "posts").FilteredEntries.First();
            vm.Explorer.SelectedEntry = post;
            await vm.StartFullPreviewCommand.ExecuteAsync(null);
            await Assert.That(vm.Preview.IsServing).IsTrue();

            const string newBody = "Edited while previewing.";
            vm.Editor.Body = newBody;
            await Assert.That(vm.Editor.IsDirty).IsTrue();

            await Task.Delay(AutoSaveSettleDelayMs);

            await Assert.That(vm.Editor.IsDirty).IsFalse();
            var written = await File.ReadAllTextAsync(vm.Editor.FilePath!);
            await Assert.That(written).Contains(newBody);
        }
        finally
        {
            if (Directory.Exists(tempParent)) Directory.Delete(tempParent, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }
}
