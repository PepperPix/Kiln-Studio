namespace Kiln.Studio.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public partial class ShellViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFolderPicker _folderPicker;

    [ObservableProperty]
    private string _title = "Kiln Studio";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string? _currentProjectPath;

    public ProjectExplorerViewModel Explorer { get; }

    public ShellViewModel(IProjectService projectService, IFolderPicker folderPicker, ProjectExplorerViewModel explorer)
    {
        _projectService = projectService;
        _folderPicker = folderPicker;
        Explorer = explorer;
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var path = await _folderPicker.PickFolderAsync("Open Kiln site").ConfigureAwait(true);
        if (path is null)
            return;

        try
        {
            var project = await Task.Run(() => _projectService.Open(path)).ConfigureAwait(true);
            Explorer.Load(project);
            CurrentProjectPath = project.ProjectPath;
            StatusMessage = $"Opened {project.SiteTitle}";
        }
        catch (ProjectOpenException ex)
        {
            StatusMessage = ex.Message;
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open project: {ex.Message}";
        }
#pragma warning restore CA1031
    }
}
