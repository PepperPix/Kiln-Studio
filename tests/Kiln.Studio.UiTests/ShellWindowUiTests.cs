using Avalonia.Headless;
using Avalonia.VisualTree;
using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

namespace Kiln.Studio.UiTests;

// Null/stub service implementations for UI tests.
// These mirror the patterns in Kiln.Studio.Tests — kept separate because
// UI tests live in a different assembly with different isolation semantics.

file sealed class NullFolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}

file sealed class NullInputDialog : IInputDialog
{
    public Task<string?> PromptAsync(string title, string message) => Task.FromResult<string?>(null);
}

file sealed class NullNewPageDialog : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames)
        => Task.FromResult<NewPageRequest?>(null);
}

file sealed class NullPreviewServer : IPreviewServer
{
    public bool IsRunning => false;
    public Uri? Url => null;
    public Task<Uri> StartAsync(string projectPath) =>
        Task.FromResult(new UriBuilder(Uri.UriSchemeHttp, "localhost", 5000).Uri);
    public void StopServer() { }
}

file sealed class NullBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri url) { }
}

file sealed class NullBuildService : IBuildService
{
    public Task<BuildSummary> BuildAsync(
        string projectPath,
        bool release,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new BuildSummary(true, 0, 0, 0, 0, projectPath, [], []));
}

file sealed class NullDeploymentService : IDeploymentService
{
    public DeploymentSetupSummary SetUp(
        string projectPath,
        DeployTarget target,
        CancellationToken cancellationToken = default) =>
        new(target, []);
}

public sealed class ShellWindowUiTests
{
    [Test]
    public async Task ShellWindow_Constructs_WithoutCrash_AndShowsWelcome()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var vm = new ShellViewModel(
                new ProjectService(new EngineHost()),
                new NullFolderPicker(),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                new ProjectExplorerViewModel(),
                new EditorViewModel(new ContentService()),
                new NullPreviewServer(),
                new NullBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService());

            var window = new ShellWindow { DataContext = vm };
            window.Show();

            // No project is open: IsProjectOpen must be false
            await Assert.That(vm.IsProjectOpen).IsFalse();

            // The WelcomeView is visible when no project is loaded
            var welcome = window.GetVisualDescendants()
                .OfType<WelcomeView>()
                .FirstOrDefault();
            await Assert.That(welcome).IsNotNull();
            await Assert.That(welcome!.IsVisible).IsTrue();

            // Stretch: capture a baseline render snapshot
            var snapshotDir = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(typeof(ShellWindowUiTests).Assembly.Location)!,
                "..", "..", "..", "Snapshots"));
            Directory.CreateDirectory(snapshotDir);
            var frame = window.CaptureRenderedFrame();
            if (frame is not null)
            {
                var snapshotPath = Path.Combine(snapshotDir, "ShellWindow_Welcome.png");
                using var stream = File.Open(snapshotPath, FileMode.Create, FileAccess.Write);
                frame.Save(stream);
            }

            window.Close();
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }
}
