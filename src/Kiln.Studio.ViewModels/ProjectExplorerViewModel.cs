namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kiln.Studio.Services;

public sealed partial class ProjectExplorerViewModel : ViewModelBase
{
    private static readonly TimeSpan DefaultSearchDebounceDelay = TimeSpan.FromMilliseconds(250);

    public ObservableCollection<ContentCollectionViewModel> Collections { get; } = [];

    private readonly TimeSpan _searchDebounceDelay;
    private Func<ContentEntryViewModel, Task>? _onToggleDraft;
    private CancellationTokenSource? _searchDebounceCts;

    [ObservableProperty]
    private ContentEntryViewModel? _selectedEntry;

    /// <summary>
    /// The collection currently shown by the full-screen content list (collection switcher,
    /// PLAN-072) - replaces the old always-all-collections TreeView. Defaults to the first loaded
    /// collection.
    /// </summary>
    [ObservableProperty]
    private ContentCollectionViewModel? _selectedCollection;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private DraftFilter _draftFilter;

    [ObservableProperty]
    private ContentSortMode _sortMode;

    public IReadOnlyList<DraftFilter> DraftFilters { get; } = Enum.GetValues<DraftFilter>();
    public IReadOnlyList<ContentSortMode> SortModes { get; } = Enum.GetValues<ContentSortMode>();

    public ProjectExplorerViewModel(TimeSpan? searchDebounceDelay = null)
    {
        _searchDebounceDelay = searchDebounceDelay ?? DefaultSearchDebounceDelay;
    }

    public void SetDraftToggleHandler(Func<ContentEntryViewModel, Task> handler) => _onToggleDraft = handler;

    public void Load(OpenedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Collections.Clear();
        foreach (var collection in project.Collections)
            Collections.Add(new ContentCollectionViewModel(collection, _onToggleDraft));
        SelectedCollection = Collections.Count > 0 ? Collections[0] : null;
        ApplyToAll();
    }

    public void Clear()
    {
        Collections.Clear();
        SelectedEntry = null;
        SelectedCollection = null;
        SearchText = null;
        DraftFilter = DraftFilter.All;
        SortMode = ContentSortMode.Default;
    }

    public void UpdateEntryDraft(string sourcePath, bool newDraft)
    {
        var collection = Collections.FirstOrDefault(c => c.HasEntry(sourcePath));
        if (collection is null)
            return;

        collection.UpdateEntry(sourcePath, newDraft);
        collection.ApplyView(SearchText, DraftFilter, SortMode);
    }

    /// <summary>
    /// Returns the taxonomy names declared for the collection that owns the given content file,
    /// or an empty list if the entry could not be matched to any loaded collection.
    /// </summary>
    public IReadOnlyList<string> GetTaxonomiesForEntry(string sourcePath) =>
        Collections.FirstOrDefault(c => c.HasEntry(sourcePath))?.Taxonomies ?? [];

    private void ApplyToAll()
    {
        foreach (var collection in Collections)
            collection.ApplyView(SearchText, DraftFilter, SortMode);
    }

    partial void OnSearchTextChanged(string? value)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();

        if (_searchDebounceDelay <= TimeSpan.Zero)
        {
            ApplyToAll();
            return;
        }

        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;
        _ = DebounceApplyToAllAsync(cts);
    }

    private async Task DebounceApplyToAllAsync(CancellationTokenSource cts)
    {
        try
        {
            // ConfigureAwait(true): intentionally resume on the captured SynchronizationContext
            // (Avalonia's UI-thread context in production) so ApplyToAll() below runs on the UI
            // thread, matching where the CommunityToolkit.Mvvm property setter itself is invoked.
            await Task.Delay(_searchDebounceDelay, cts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested)
            return;

        ApplyToAll();
    }

    partial void OnDraftFilterChanged(DraftFilter value) => ApplyToAll();

    partial void OnSortModeChanged(ContentSortMode value) => ApplyToAll();
}
