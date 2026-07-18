namespace Kiln.Studio.UiTests;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;
using Kiln.Studio.Views;

/// <summary>
/// Bug fix regression (2026-07-13): a right-click on a content list row used to also select the
/// entry (Avalonia's ListBoxItem selects on ANY pointer-button press, including right-click),
/// which made ShellViewModel immediately load the item into the editor instead of just showing
/// the row's ContextMenu. <see cref="ContentListView"/>'s code-behind now intercepts right-clicks
/// at the Tunnel routing phase to suppress the selection change while leaving the ContextMenu
/// untouched. This test drives a real pointer press/release through the headless input pipeline
/// against a realized ListBoxItem to verify the fix end-to-end (not just that the code compiles).
/// </summary>
public sealed class ContentListRightClickUiTests
{
    [Test]
    public async Task RightClickOnEntry_DoesNotChangeSelection_ButLeftClickDoes()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(parentDir);
        var storeDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(storeDir);

        try
        {
            var projectService = new ProjectService(new EngineHost());
            var sitePath = projectService.CreateSite(parentDir, "my-blog");
            SeedTwoPosts(sitePath);

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

            Dispatcher.UIThread.RunJobs();
            window.CaptureRenderedFrame();

            var listBox = window.GetVisualDescendants().OfType<ListBox>()
                .FirstOrDefault(l => l.Name == "EntryListBox");
            await Assert.That(listBox).IsNotNull();

            var item = listBox!.GetVisualDescendants().OfType<ListBoxItem>().FirstOrDefault();
            await Assert.That(item).IsNotNull();

            var center = new Point(item!.Bounds.Width / 2, item.Bounds.Height / 2);
            var windowPoint = item.TranslatePoint(center, window);
            await Assert.That(windowPoint).IsNotNull();
            var wp = windowPoint!.Value;

            // (a) Right-click must NOT change SelectedEntry (the actual bug being fixed).
            window.MouseDown(wp, MouseButton.Right);
            window.MouseUp(wp, MouseButton.Right);
            Dispatcher.UIThread.RunJobs();

            await Assert.That(explorer.SelectedEntry).IsNull();

            // (b) Left-click on the same row DOES select it (selection still works normally).
            window.MouseDown(wp, MouseButton.Left);
            window.MouseUp(wp, MouseButton.Left);
            Dispatcher.UIThread.RunJobs();

            await Assert.That(explorer.SelectedEntry).IsNotNull();

            window.Close();
        }
        finally
        {
            DirectoryHelper.TryDeleteRecursive(parentDir);
            DirectoryHelper.TryDeleteRecursive(storeDir);
        }
    }

    private static void SeedTwoPosts(string sitePath)
    {
        var postsDir = Path.Combine(sitePath, "content", "posts");
        Directory.CreateDirectory(postsDir);

        File.WriteAllText(Path.Combine(postsDir, "first-post.md"), """
            ---
            title: "First Post"
            date: 2026-07-01
            draft: false
            ---

            Body of the first post.
            """);

        File.WriteAllText(Path.Combine(postsDir, "second-post.md"), """
            ---
            title: "Second Post"
            date: 2026-07-02
            draft: false
            ---

            Body of the second post.
            """);
    }

}
