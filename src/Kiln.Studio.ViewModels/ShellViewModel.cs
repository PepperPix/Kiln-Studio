namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public partial class ShellViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly IFolderPicker _folderPicker;
    private readonly IInputDialog _inputDialog;
    private readonly IRecentProjectsStore _recentProjectsStore;
    private readonly IContentService _contentService;
    private readonly INewPageDialog _newPageDialog;
    private readonly IPreviewServer _previewServer;
    private readonly IBrowserLauncher _browserLauncher;

    [ObservableProperty]
    private string _title = "Kiln Studio";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string? _currentProjectPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartFullPreviewCommand))]
    private bool _isProjectOpen;

    public ProjectExplorerViewModel Explorer { get; }
    public EditorViewModel Editor { get; }
    public PreviewViewModel Preview { get; }

    public ObservableCollection<RecentProjectViewModel> RecentProjects { get; } = [];

#pragma warning disable S107
    public ShellViewModel(
        IProjectService projectService,
        IFolderPicker folderPicker,
        IInputDialog inputDialog,
        IRecentProjectsStore recentProjectsStore,
        IContentService contentService,
        INewPageDialog newPageDialog,
        ProjectExplorerViewModel explorer,
        EditorViewModel editor,
        IPreviewServer previewServer,
        IBrowserLauncher browserLauncher,
        PreviewViewModel preview)
#pragma warning restore S107
    {
        _projectService = projectService;
        _folderPicker = folderPicker;
        _inputDialog = inputDialog;
        _recentProjectsStore = recentProjectsStore;
        _contentService = contentService;
        _newPageDialog = newPageDialog;
        _previewServer = previewServer;
        _browserLauncher = browserLauncher;
        Explorer = explorer;
        Editor = editor;
        Preview = preview;

        Explorer.PropertyChanged += OnExplorerPropertyChanged;
        RefreshRecentProjects();
    }

    private void OnExplorerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProjectExplorerViewModel.SelectedEntry))
            return;
        if (Explorer.SelectedEntry is null)
            return;

        if (Editor.IsDirty)
            StatusMessage = "Unsaved changes were discarded.";

        Editor.Load(Explorer.SelectedEntry.SourcePath);
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

    [RelayCommand]
    private async Task NewPageAsync()
    {
        if (!IsProjectOpen || Explorer.Collections.Count == 0)
            return;

        var collectionNames = Explorer.Collections.Select(c => c.Name).ToList();
        var req = await _newPageDialog.ShowAsync(collectionNames).ConfigureAwait(true);
        if (req is null)
            return;

        var collection = Explorer.Collections.FirstOrDefault(c => c.Name == req.CollectionName);
        if (collection is null)
            return;

        var contentDir = collection.ContentDirectory;

        try
        {
            var path = await Task.Run(() => _contentService.CreatePage(contentDir, req.Title)).ConfigureAwait(true);
            await OpenPathAsync(CurrentProjectPath!).ConfigureAwait(true);
            Editor.Load(path);
            StatusMessage = $"Created {Path.GetFileName(path)}";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create page: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    internal async Task OpenPathAsync(string path)
    {
        StopFullPreview();
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

    [RelayCommand(CanExecute = nameof(CanServe))]
    private async Task StartFullPreviewAsync()
    {
        try
        {
            var url = await _previewServer.StartAsync(CurrentProjectPath!).ConfigureAwait(true);
            _browserLauncher.Open(url);
            Preview.IsServing = true;
            Preview.ServeStatus = $"Serving at {url}";
            StatusMessage = Preview.ServeStatus;
            StartFullPreviewCommand.NotifyCanExecuteChanged();
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            Preview.ServeStatus = $"Preview failed: {ex.Message}";
            StatusMessage = Preview.ServeStatus;
        }
#pragma warning restore CA1031
    }

    private bool CanServe() => IsProjectOpen && !Preview.IsServing;

    [RelayCommand]
    private void StopFullPreview()
    {
        _previewServer.StopServer();
        Preview.IsServing = false;
        Preview.ServeStatus = "Preview stopped";
        StatusMessage = Preview.ServeStatus;
        StartFullPreviewCommand.NotifyCanExecuteChanged();
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
