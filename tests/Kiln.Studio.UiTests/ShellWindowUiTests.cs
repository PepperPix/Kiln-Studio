using System.Runtime.InteropServices;
using Avalonia.VisualTree;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

namespace Kiln.Studio.UiTests;

public sealed class ShellWindowUiTests
{
    [Test]
    public async Task ShellWindow_Constructs_WithoutCrash_AndShowsWelcome()
    {
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var window = BuildShellWindow(storeDir);
            window.Show();

            var vm = (ShellViewModel)window.DataContext!;

            // No project is open: IsProjectOpen must be false
            await Assert.That(vm.IsProjectOpen).IsFalse();

            // The WelcomeView is visible when no project is loaded
            var welcome = window.GetVisualDescendants()
                .OfType<WelcomeView>()
                .FirstOrDefault();
            await Assert.That(welcome).IsNotNull();
            await Assert.That(welcome!.IsVisible).IsTrue();

            window.Close();
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    /// <summary>
    /// Snapshot baseline: Welcome screen.
    /// Platform-gated — reference platform is macOS arm64 (ADR-030).
    /// On first run (or KILN_UPDATE_SNAPSHOTS=1) the baseline is written; subsequent
    /// runs compare against it with ≤0.1 % tolerance.
    /// </summary>
    [Test]
    public async Task Snapshot_Welcome_MatchesBaseline()
    {
        if (!IsMacOsArm64()) return;

        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);
        try
        {
            var window = BuildShellWindow(storeDir);
            window.Show();

            await SnapshotComparer.AssertMatchesBaseline(window, "ShellWindow_Welcome");

            window.Close();
        }
        finally
        {
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static ShellWindow BuildShellWindow(string storeDir)
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
            new NullDeploymentService(),
            new NullSettingsDialog());

        return new ShellWindow { DataContext = vm };
    }

    private static bool IsMacOsArm64() =>
        OperatingSystem.IsMacOS() &&
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
}
