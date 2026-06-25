namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public partial class ShellViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFolderPicker _folderPicker;
    private readonly IInputDialog _inputDialog;
    private readonly IRecentProjectsStore _recentProjectsStore;

    [ObservableProperty]
    private string _title = "Kiln Studio";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string? _currentProjectPath;

    [ObservableProperty]
    private bool _isProjectOpen;

    public ProjectExplorerViewModel Explorer { get; }

    public ObservableCollection<RecentProjectViewModel> RecentProjects { get; } = [];

    public ShellViewModel(
        IProjectService projectService,
        IFolderPicker folderPicker,
        IInputDialog inputDialog,
        IRecentProjectsStore recentProjectsStore,
        ProjectExplorerViewModel explorer)
    {
        _projectService = projectService;
        _folderPicker = folderPicker;
        _inputDialog = inputDialog;
        _recentProjectsStore = recentProjectsStore;
        Explorer = explorer;

        RefreshRecentProjects();
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var path = await _folderPicker.PickFolderAsync("Open Kiln site").ConfigureAwait(true);
        if (path is null)
            return;

        await OpenPathAsync(path).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task NewSiteAsync()
    {
        var parent = await _folderPicker.PickFolderAsync("Choose location for new site").ConfigureAwait(true);
        if (parent is null)
            return;

        var name = await _inputDialog.PromptAsync("New site", "Site name:").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            var path = await Task.Run(() => _projectService.CreateSite(parent, name)).ConfigureAwait(true);
            await OpenPathAsync(path).ConfigureAwait(true);
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create site: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    internal async Task OpenPathAsync(string path)
    {
        try
        {
            var project = await Task.Run(() => _projectService.Open(path)).ConfigureAwait(true);
            Explorer.Load(project);
            CurrentProjectPath = project.ProjectPath;
            StatusMessage = $"Opened {project.SiteTitle}";
            IsProjectOpen = true;
            _recentProjectsStore.Add(project.ProjectPath, project.SiteTitle);
            RefreshRecentProjects();
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

    private void RefreshRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var rp in _recentProjectsStore.GetAll())
            RecentProjects.Add(new RecentProjectViewModel(
                rp.Name,
                rp.Path,
                new AsyncRelayCommand(() => OpenPathAsync(rp.Path))));
    }
}
