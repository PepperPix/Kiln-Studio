namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using Kiln.Studio.Services;

public sealed class ContentCollectionViewModel : ViewModelBase
{
    public string Name { get; }
    public string ContentDirectory { get; }
    public IReadOnlyList<string> Taxonomies { get; }
    public ObservableCollection<ContentEntryViewModel> FilteredEntries { get; } = [];
    public int VisibleCount => FilteredEntries.Count;

    private readonly List<ContentEntry> _dtoEntries;
    private readonly Dictionary<string, ContentEntryViewModel> _entryMap = [];

    public ContentCollectionViewModel(ContentCollectionDto dto, Func<ContentEntryViewModel, Task>? onToggleDraft = null)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Name = dto.Name;
        ContentDirectory = dto.ContentDirectory;
        Taxonomies = dto.Taxonomies;
        _dtoEntries = dto.Entries.ToList();
        foreach (var entry in dto.Entries)
        {
            var vm = new ContentEntryViewModel(entry, onToggleDraft);
            _entryMap[entry.SourcePath] = vm;
            FilteredEntries.Add(vm);
        }
    }

    public void ApplyView(string? searchText, DraftFilter draftFilter, ContentSortMode sortMode)
    {
        var filtered = ContentQuery.Apply(_dtoEntries, searchText, draftFilter, sortMode);
        FilteredEntries.Clear();
        foreach (var entry in filtered)
            FilteredEntries.Add(_entryMap[entry.SourcePath]);
        OnPropertyChanged(nameof(VisibleCount));
    }

    public bool HasEntry(string sourcePath) => _entryMap.ContainsKey(sourcePath);

    public void UpdateEntry(string sourcePath, bool newDraft)
    {
        var idx = _dtoEntries.FindIndex(e => e.SourcePath == sourcePath);
        if (idx < 0)
            return;

        _dtoEntries[idx] = _dtoEntries[idx] with { Draft = newDraft };

        if (_entryMap.TryGetValue(sourcePath, out var vm))
            vm.Draft = newDraft;
    }
}
