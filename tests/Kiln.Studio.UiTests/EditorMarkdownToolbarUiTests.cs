namespace Kiln.Studio.UiTests;

using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

/// <summary>
/// PLAN-066: seeds a post, opens it in the editor, and drives each Markdown-toolbar button
/// (Bold/Italic/Code/Link/Heading/Bulleted List) via a real Button.ClickEvent raise (headless-safe
/// — no pointer coordinates needed) against a selection set directly on the AvaloniaEdit
/// TextEditor. Asserts the resulting BodyEditor.Text after each action. Also dumps a PNG under
/// Snapshots/__review__/ showing the toolbar for manual visual review.
///
/// Platform-gated (ADR-030 reference platform: macOS arm64).
/// </summary>
public sealed class EditorMarkdownToolbarUiTests
{
    private const string PostTitle = "Toolbar Sample";

    private const string SeedBody = """
        Bold Italic Code Link CodeBlockContent

        Heading line

        Item one
        Item two

        Ordered one
        Ordered two

        Quote line
        """;

    [Test]
    public async Task ToolbarButtons_ApplyMarkdownSyntax_ToSelection()
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
                new NullSettingsDialog(),
                new NullDeploymentConfigStore(),
                new NullPublishService(),
                new FakeContentFrontmatterWriter());

            var window = new ShellWindow { DataContext = vm, Width = 1200, Height = 760 };
            window.Show();

            await vm.OpenProjectCommand.ExecuteAsync(null);
            var post = explorer.Collections.First(c => c.Name == "posts")
                .FilteredEntries.First(e => e.Title == PostTitle);
            explorer.SelectedEntry = post;
            await Assert.That(editor.HasDocument).IsTrue();
            Dispatcher.UIThread.RunJobs();

            var bodyEditor = window.GetVisualDescendants().OfType<TextEditor>().First(t => t.Name == "BodyEditor");
            var boldButton = FindButton(window, "BoldButton");
            var italicButton = FindButton(window, "ItalicButton");
            var codeButton = FindButton(window, "CodeButton");
            var codeBlockButton = FindButton(window, "CodeBlockButton");
            var linkButton = FindButton(window, "LinkButton");
            var headingButton = FindButton(window, "HeadingButton");
            var bulletListButton = FindButton(window, "BulletListButton");
            var numberedListButton = FindButton(window, "NumberedListButton");
            var blockquoteButton = FindButton(window, "BlockquoteButton");

            Capture(window, reviewDir, "08_editor_markdown_toolbar");

            SelectWord(bodyEditor, "Bold");
            Click(boldButton);
            await Assert.That(bodyEditor.Text).Contains("**Bold**");

            SelectWord(bodyEditor, "Italic");
            Click(italicButton);
            await Assert.That(bodyEditor.Text).Contains("_Italic_");

            SelectWord(bodyEditor, "Code");
            Click(codeButton);
            await Assert.That(bodyEditor.Text).Contains("`Code`");

            SelectWord(bodyEditor, "Link");
            Click(linkButton);
            await Assert.That(bodyEditor.Text).Contains("[Link](url)");
            var linkSelectionText = bodyEditor.Document.GetText(bodyEditor.SelectionStart, bodyEditor.SelectionLength);
            await Assert.That(linkSelectionText).IsEqualTo("url");

            PlaceCaretOnLine(bodyEditor, "Heading line");
            Click(headingButton);
            await Assert.That(bodyEditor.Text).Contains("## Heading line");

            SelectLines(bodyEditor, "Item one", "Item two");
            Click(bulletListButton);
            await Assert.That(bodyEditor.Text).Contains("- Item one");
            await Assert.That(bodyEditor.Text).Contains("- Item two");

            SelectLines(bodyEditor, "Ordered one", "Ordered two");
            Click(numberedListButton);
            await Assert.That(bodyEditor.Text).Contains("1. Ordered one");
            await Assert.That(bodyEditor.Text).Contains("2. Ordered two");

            PlaceCaretOnLine(bodyEditor, "Quote line");
            Click(blockquoteButton);
            await Assert.That(bodyEditor.Text).Contains("> Quote line");

            SelectWord(bodyEditor, "CodeBlockContent");
            Click(codeBlockButton);
            await Assert.That(bodyEditor.Text).Contains("```\nCodeBlockContent\n```");

            // Round-trip through the ViewModel's Save path to confirm the toolbar mutations were
            // real Document edits (not something bypassing the existing BodyEditor<->Body sync).
            await Assert.That(editor.Body).IsEqualTo(bodyEditor.Text);

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
            draft: false
            ---

            {SeedBody}
            """;
        File.WriteAllText(Path.Combine(postsDir, "toolbar-sample.md"), content);
    }

    private static Button FindButton(Window window, string name) =>
        window.GetVisualDescendants().OfType<Button>().First(b => b.Name == name);

    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    private static void SelectWord(TextEditor editor, string word)
    {
        var offset = editor.Text.IndexOf(word, StringComparison.Ordinal);
        // Select(start, length) atomically replaces both, avoiding a transient state where the
        // still-stale SelectionLength from a previous action (if set first) could push past the
        // document's end when combined with this action's new offset.
        editor.Select(offset, word.Length);
    }

    private static void PlaceCaretOnLine(TextEditor editor, string lineText)
    {
        var offset = editor.Text.IndexOf(lineText, StringComparison.Ordinal);
        editor.Select(offset, 0);
    }

    private static void SelectLines(TextEditor editor, string firstLineText, string lastLineText)
    {
        var start = editor.Text.IndexOf(firstLineText, StringComparison.Ordinal);
        var lastLineStart = editor.Text.IndexOf(lastLineText, StringComparison.Ordinal);
        var end = lastLineStart + lastLineText.Length;
        editor.Select(start, end - start);
    }

    private static void Capture(Window window, string reviewDir, string name)
    {
        Avalonia.Media.Imaging.WriteableBitmap frame = window.CaptureRenderedFrame()
            ?? throw new InvalidOperationException($"CaptureRenderedFrame returned null for '{name}'.");

        using var stream = File.Open(Path.Combine(reviewDir, $"{name}.png"), FileMode.Create, FileAccess.Write);
        frame.Save(stream);
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
