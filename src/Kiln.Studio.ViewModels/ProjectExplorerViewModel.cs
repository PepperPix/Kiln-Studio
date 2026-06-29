namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kiln.Studio.Services;

public sealed partial class ProjectExplorerViewModel : ViewModelBase
{
    public ObservableCollection<ContentCollectionViewModel> Collections { get; } = [];

    [ObservableProperty]
    private ContentEntryViewModel? _selectedEntry;

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private DraftFilter _draftFilter;

    [ObservableProperty]
    private ContentSortMode _sortMode;

    public IReadOnlyList<DraftFilter> DraftFilters { get; } = Enum.GetValues<DraftFilter>();
    public IReadOnlyList<ContentSortMode> SortModes { get; } = Enum.GetValues<ContentSortMode>();

    public void Load(OpenedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Collections.Clear();
        foreach (var collection in project.Collections)
            Collections.Add(new ContentCollectionViewModel(collection));
        ApplyToAll();
    }

    public void Clear()
    {
        Collections.Clear();
        SelectedEntry = null;
        SearchText = null;
        DraftFilter = DraftFilter.All;
        SortMode = ContentSortMode.Default;
    }

    private void ApplyToAll()
    {
        foreach (var collection in Collections)
            collection.ApplyView(SearchText, DraftFilter, SortMode);
    }

    partial void OnSearchTextChanged(string? value) => ApplyToAll();

    partial void OnDraftFilterChanged(DraftFilter value) => ApplyToAll();

    partial void OnSortModeChanged(ContentSortMode value) => ApplyToAll();
}
