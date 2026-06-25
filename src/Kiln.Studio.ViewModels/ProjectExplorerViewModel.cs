namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kiln.Studio.Services;

public sealed partial class ProjectExplorerViewModel : ViewModelBase
{
    public ObservableCollection<ContentCollectionViewModel> Collections { get; } = [];

    [ObservableProperty]
    private ContentEntryViewModel? _selectedEntry;

    public void Load(OpenedProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Collections.Clear();
        foreach (var collection in project.Collections)
            Collections.Add(new ContentCollectionViewModel(collection));
    }
}
