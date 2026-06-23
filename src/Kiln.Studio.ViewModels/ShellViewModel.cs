namespace Kiln.Studio.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class ShellViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _title = "Kiln Studio";

    [RelayCommand]
    private void OpenProject()
    {
        // No-op stub — file dialog and project loading implemented in a later plan.
        _ = Title;
    }
}
