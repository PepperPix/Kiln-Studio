namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using Kiln.Studio.Services;

public sealed class ContentCollectionViewModel : ViewModelBase
{
    public string Name { get; }
    public ObservableCollection<ContentEntryViewModel> Entries { get; } = [];

    public ContentCollectionViewModel(ContentCollectionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Name = dto.Name;
        foreach (var entry in dto.Entries)
            Entries.Add(new ContentEntryViewModel(entry));
    }
}
