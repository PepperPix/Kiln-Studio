namespace Kiln.Studio.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public sealed partial class ContentEntryViewModel : ViewModelBase
{
    private readonly Func<ContentEntryViewModel, Task>? _onToggleDraft;

    public string Title { get; }
    public string SourcePath { get; }
    public DateTime? Date { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ToggleDraftHeader))]
    private bool _draft;

    public string ToggleDraftHeader => Draft ? "Unmark draft" : "Mark as draft";

    public ContentEntryViewModel(ContentEntry entry, Func<ContentEntryViewModel, Task>? onToggleDraft = null)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _onToggleDraft = onToggleDraft;
        Title = entry.Title;
        SourcePath = entry.SourcePath;
        _draft = entry.Draft;
        Date = entry.Date;
    }

    [RelayCommand]
    private async Task ToggleDraftAsync()
    {
        if (_onToggleDraft is not null)
            await _onToggleDraft(this).ConfigureAwait(true);
    }
}
