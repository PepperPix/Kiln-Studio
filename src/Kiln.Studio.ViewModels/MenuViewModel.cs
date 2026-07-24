namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Kiln.Studio.Services;

public sealed partial class MenuViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _error;

    public ObservableCollection<MenuItemViewModel> Items { get; } = [];

    public bool HasError => !string.IsNullOrEmpty(Error);

    public MenuViewModel(MenuDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        _name = definition.Name;
        foreach (var item in definition.Items)
            Items.Add(new MenuItemViewModel(item, null));
    }

    public MenuDefinition ToDefinition()
    {
        return new MenuDefinition(
            Name,
            Items.Select(i => i.ToDefinition()).ToList());
    }
}
