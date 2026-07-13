namespace Kiln.Studio.UiTests;

using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

/// <summary>
/// Exploratory "click-through" check (NOT a regression/snapshot test — no baseline comparison,
/// no strict pass/fail on pixels) for PLAN-058: seeds a post whose Markdown body exercises a
/// heading, bold/italic text, a fenced code block and a link, opens it in the editor, and dumps a
/// PNG under Snapshots/__review__/ for manual review that AvaloniaEdit.TextMate now renders
/// visually distinct colors/weights instead of uniform plain text.
///
/// Platform-gated (ADR-030 reference platform: macOS arm64) — see ExploratoryTourUiTests for the
/// same headless-rendering-portability rationale.
/// </summary>
public sealed class EditorSyntaxHighlightingUiTests
{
    [Test]
    public async Task Tour_MarkdownFormatting_CapturesHighlightedEditor()
    {
        if (!IsMacOsArm64())
            return;

        var reviewDir = ResolveReviewDir();
        Directory.CreateDirectory(reviewDir);

        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            SeedFormattedPost(sitePath);

            var explorer = new ProjectExplorerViewModel();
            var editor = new EditorViewModel(new ContentService());

            var vm = new ShellViewModel(
                projectService,
                new FixedFolderPicker(sitePath),
                new NullInputDialog(),
                new RecentProjectsStore(storeDir),
                new ContentService(),
                new NullNewPageDialog(),
                explorer,
                editor,
                new NullPreviewServer(),
                new NullBrowserLauncher(),
                new PreviewViewModel(),
                new NullBuildService(),
                new NullDeploymentService(),
                new SettingsViewModel(new FakeSiteSettingsService(), new NullDeploymentConfigStore()),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter());

            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            await Assert.That(vm.IsProjectOpen).IsTrue();

            var post = explorer.Collections.First(c => c.Name == "posts")
                .FilteredEntries.First(e => e.Title == "Formatting sample");
            explorer.SelectedEntry = post;
            await Assert.That(editor.HasDocument).IsTrue();

            Capture(window, reviewDir, "05_editor_markdown_syntax_highlighting");

            window.Close();
        }
        finally
        {
            if (Directory.Exists(parentDir)) Directory.Delete(parentDir, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void SeedFormattedPost(string sitePath)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);

        const string content = """
            ---
            title: "Formatting sample"
            date: 2026-01-01
            draft: false
            ---

            # Heading One

            This paragraph has **bold text** and _italic text_ to check TextMate highlighting.

            ```csharp
            var x = 42;
            ```

            See the [Kiln docs](https://example.com) for more.
            """;
        File.WriteAllText(Path.Combine(postsDir, "formatting-sample.md"), content);
    }

    private static void Capture(Window window, string reviewDir, string name)
    {
        WriteableBitmap frame = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException($"CaptureRenderedFrame returned null for '{name}'.");

        using var stream = File.Open(Path.Combine(reviewDir, $"{name}.png"), FileMode.Create, FileAccess.Write);
        frame.Save(stream, PngBitmapEncoderOptions.Default);
    }

    private static string ResolveReviewDir([System.Runtime.CompilerServices.CallerFilePath] string callerFile = "")
    {
        var testDir = Path.GetDirectoryName(callerFile)!;
        return Path.GetFullPath(Path.Combine(testDir, "Snapshots", "__review__"));
    }

    private static bool IsMacOsArm64() =>
        OperatingSystem.IsMacOS() &&
        RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
}
