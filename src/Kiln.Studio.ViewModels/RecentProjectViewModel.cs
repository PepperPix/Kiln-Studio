namespace Kiln.Studio.ViewModels;

using CommunityToolkit.Mvvm.Input;

public sealed class RecentProjectViewModel : ViewModelBase
{
    public string Name { get; }
    public string Path { get; }
    public IAsyncRelayCommand OpenCommand { get; }

    internal RecentProjectViewModel(string name, string path, IAsyncRelayCommand openCommand)
    {
        Name = name;
        Path = path;
        OpenCommand = openCommand;
    }
}
