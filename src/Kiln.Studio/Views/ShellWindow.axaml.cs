using Avalonia.Controls;
using Kiln.Studio.ViewModels;

namespace Kiln.Studio.Views;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
        var explorer = this.FindControl<TreeView>("ExplorerTree");
        if (explorer is not null)
            explorer.SelectionChanged += OnExplorerSelectionChanged;
    }

    private void OnExplorerSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ShellViewModel vm && sender is TreeView tree)
            vm.Explorer.SelectedEntry = tree.SelectedItem as ContentEntryViewModel;
    }
}
