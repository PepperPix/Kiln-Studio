namespace Kiln.Studio.UiTests;

using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

/// <summary>
/// PLAN-065: seeds a post with title/date/description, opens it in the editor and asserts that
/// the structured form fields (Title TextBox, DatePicker, Description TextBox) are populated and
/// that the raw-YAML text box (behind a ToggleButton disclosure, not an Expander — see the
/// styling follow-up on 2026-07-09) is collapsed by default. Also dumps a PNG under
/// Snapshots/__review__/ for manual visual review (not a pixel-diff regression test — see
/// ExploratoryTourUiTests for the same headless-rendering-portability rationale).
///
/// Platform-gated (ADR-030 reference platform: macOS arm64).
/// </summary>
public sealed class EditorFrontmatterFormUiTests
{
    private const string PostTitle = "Frontmatter Form Sample";
    private const string PostDescription = "A short summary used to verify the description field.";
    private const int RightPanelColumnIndex = 2;

    [Test]
    public async Task OpenPost_ShowsStructuredFormFields_AndCollapsedRawYaml()
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
            SeedPost(sitePath);

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
                .FilteredEntries.First(e => e.Title == PostTitle);
            explorer.SelectedEntry = post;
            await Assert.That(editor.HasDocument).IsTrue();

            // Let the layout/binding pass apply the freshly-loaded ViewModel values to the
            // realized controls before querying the visual tree for them.
            Dispatcher.UIThread.RunJobs();

            // Structured fields reflect the loaded values.
            await Assert.That(editor.Title).IsEqualTo(PostTitle);
            await Assert.That(editor.Date).IsNotNull();
            await Assert.That(editor.Description).IsEqualTo(PostDescription);

            var titleBox = window.GetVisualDescendants().OfType<TextBox>()
                .FirstOrDefault(t => t.Text == PostTitle);
            await Assert.That(titleBox).IsNotNull();

            var datePicker = window.GetVisualDescendants().OfType<DatePicker>().FirstOrDefault();
            await Assert.That(datePicker).IsNotNull();
            await Assert.That(datePicker!.SelectedDate).IsNotNull();

            var rawToggle = window.GetVisualDescendants().OfType<ToggleButton>()
                .FirstOrDefault(t => t.Name == "RawFrontMatterToggle");
            await Assert.That(rawToggle).IsNotNull();
            await Assert.That(rawToggle!.IsChecked).IsFalse();

            var frontMatterBox = window.GetVisualDescendants().OfType<TextBox>()
                .FirstOrDefault(t => t.Name == "FrontMatterBox");
            await Assert.That(frontMatterBox).IsNotNull();
            await Assert.That(frontMatterBox!.IsEffectivelyVisible).IsFalse();

            Capture(window, reviewDir, "06_editor_frontmatter_form_collapsed_raw_yaml");

            // Collapsing the unified right panel (not just the raw-YAML disclosure) must
            // reclaim the space for the body editor instead of leaving an empty gap — see
            // ADR-056/PLAN-074: the panel toggle resets DocumentGrid's right column width to 0
            // (not just hiding the ScrollViewer), so no dead space remains.
            var panelToggle = window.GetVisualDescendants().OfType<ToggleButton>()
                .FirstOrDefault(t => t.Name == "RightPanelToggle");
            await Assert.That(panelToggle).IsNotNull();
            await Assert.That(panelToggle!.IsChecked).IsTrue();

            panelToggle.IsChecked = false;
            Dispatcher.UIThread.RunJobs();

            var documentGrid = window.GetVisualDescendants().OfType<Grid>()
                .FirstOrDefault(g => g.Name == "DocumentGrid");
            await Assert.That(documentGrid).IsNotNull();
            await Assert.That(documentGrid!.ColumnDefinitions[RightPanelColumnIndex].Width.Value).IsEqualTo(0);

            Capture(window, reviewDir, "07_editor_right_panel_collapsed_full_height_body");

            window.Close();
        }
        finally
        {
            if (Directory.Exists(parentDir)) Directory.Delete(parentDir, recursive: true);
            if (Directory.Exists(storeDir)) Directory.Delete(storeDir, recursive: true);
        }
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void SeedPost(string sitePath)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);

        const string content = $"""
            ---
            title: "{PostTitle}"
            date: 2026-07-09
            description: "{PostDescription}"
            draft: false
            ---

            Body of the frontmatter-form sample post.
            """;
        File.WriteAllText(Path.Combine(postsDir, "frontmatter-form-sample.md"), content);
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
