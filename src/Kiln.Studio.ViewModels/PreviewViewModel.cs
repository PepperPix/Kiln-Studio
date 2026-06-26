namespace Kiln.Studio.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public partial class PreviewViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isServing;

    [ObservableProperty]
    private string _serveStatus = "";
}
