namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.ViewModels;

public sealed class ContentEntryViewModelTests
{
    [Test]
    public async Task ToggleDraftCommand_CallsDelegateWithSelf()
    {
        ContentEntryViewModel? receivedEntry = null;
        var vm = new ContentEntryViewModel(
            new ContentEntry("Test", "/path/to/test.md", false, null),
            entry =>
            {
                receivedEntry = entry;
                return Task.CompletedTask;
            });

        await vm.ToggleDraftCommand.ExecuteAsync(null);

        await Assert.That(receivedEntry).IsNotNull();
        await Assert.That(receivedEntry!.SourcePath).IsEqualTo("/path/to/test.md");
        await Assert.That(receivedEntry.Title).IsEqualTo("Test");
    }

    [Test]
    public async Task ToggleDraftCommand_NoDelegate_DoesNotThrow()
    {
        var vm = new ContentEntryViewModel(
            new ContentEntry("Test", "/path/to/test.md", false, null));

        await vm.ToggleDraftCommand.ExecuteAsync(null);

        await Assert.That(vm.Title).IsEqualTo("Test");
    }

    [Test]
    public async Task ToggleDraftHeader_WhenDraftIsFalse_ShowsMarkAsDraft()
    {
        var vm = new ContentEntryViewModel(
            new ContentEntry("Test", "/path.md", false, null));

        await Assert.That(vm.ToggleDraftHeader).IsEqualTo("Mark as draft");
    }

    [Test]
    public async Task ToggleDraftHeader_WhenDraftIsTrue_ShowsUnmarkDraft()
    {
        var vm = new ContentEntryViewModel(
            new ContentEntry("Test", "/path.md", true, null));

        await Assert.That(vm.ToggleDraftHeader).IsEqualTo("Unmark draft");
    }
}
