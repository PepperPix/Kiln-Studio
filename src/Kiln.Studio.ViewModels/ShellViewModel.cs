namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;

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
    private readonly IFolderRevealer _folderRevealer;
    private readonly IBuildService _buildService;
    private readonly IDeploymentService _deploymentService;
    private readonly ISettingsDialog _settingsDialog;
    private readonly IDeploymentConfigStore _deploymentConfigStore;
    private readonly IPublishService _publishService;
    private readonly IContentFrontmatterWriter _contentFrontmatterWriter;
    private readonly IUnsavedChangesDialog _unsavedChangesDialog;
    private static readonly TimeSpan PreviewAutoSaveDebounceDelay = TimeSpan.FromSeconds(1);
    private CancellationTokenSource? _previewAutoSaveCts;

    [ObservableProperty]
    private string _title = "Kiln Studio";

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInFileManagerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private string? _currentProjectPath;

    [ObservableProperty]
    private string? _currentProjectName;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartFullPreviewCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSettingsCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseProjectCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenInFileManagerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isProjectOpen;

    [ObservableProperty]
    private bool _releaseBuild;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetUpGitHubPagesCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetUpAzureStaticWebAppsCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenInFileManagerCommand))]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanPublish))]
    private DeploymentVariant _currentDeploymentVariant;

    public bool CanPublish => IsProjectOpen && !IsBusy && CurrentDeploymentVariant == DeploymentVariant.Filesystem;

    public bool IsCiVariant => CurrentDeploymentVariant is DeploymentVariant.GitHubPages or DeploymentVariant.AzureStaticWebApps;

    public bool HasDeploymentVariant => CurrentDeploymentVariant != DeploymentVariant.None;

    public bool NoDeploymentVariant => CurrentDeploymentVariant == DeploymentVariant.None;

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
        PreviewViewModel preview,
        IBuildService buildService,
        IDeploymentService deploymentService,
        ISettingsDialog settingsDialog,
        IDeploymentConfigStore deploymentConfigStore,
        IPublishService publishService,
        IContentFrontmatterWriter contentFrontmatterWriter,
        IFolderRevealer? folderRevealer = null,
        IUnsavedChangesDialog? unsavedChangesDialog = null)
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
        _folderRevealer = folderRevealer ?? new NullFolderRevealer();
        _buildService = buildService;
        _deploymentService = deploymentService;
        _settingsDialog = settingsDialog;
        _deploymentConfigStore = deploymentConfigStore;
        _publishService = publishService;
        _contentFrontmatterWriter = contentFrontmatterWriter;
        _unsavedChangesDialog = unsavedChangesDialog ?? new AutoDiscardUnsavedChangesDialog();
        Explorer = explorer;
        Explorer.SetDraftToggleHandler(ToggleDraftAsync);
        Editor = editor;
        Editor.SetPageBundleConvertedHandler(HandlePageBundleConvertedAsync);
        Editor.PropertyChanged += OnEditorPropertyChangedForPreviewAutoSave;
        Preview = preview;

        Explorer.PropertyChanged += OnExplorerPropertyChanged;
        RefreshRecentProjects();
    }

#pragma warning disable VSTHRD100 // must match PropertyChangedEventHandler's void signature
    private async void OnExplorerPropertyChanged(object? sender, PropertyChangedEventArgs e)
#pragma warning restore VSTHRD100
    {
        if (e.PropertyName != nameof(ProjectExplorerViewModel.SelectedEntry))
            return;
        if (Explorer.SelectedEntry is null)
            return;

        // No Cancel option here: the tree selection has already visually changed, and there is no
        // clean way to revert it from the view model (see PLAN-060-Nachfolge-ADR discussion). The
        // dialog still gives the user a chance to Save instead of silently losing the old document.
        if (!await ResolveUnsavedChangesAsync(allowCancel: false).ConfigureAwait(true))
            return;

        var sourcePath = Explorer.SelectedEntry.SourcePath;
        Editor.Load(sourcePath, CurrentProjectPath, Explorer.GetTaxonomiesForEntry(sourcePath));
    }

    /// <summary>
    /// If the currently-open document is dirty, asks the user whether to save or discard it before
    /// an action (switching content/project, closing) proceeds.
    /// </summary>
    /// <returns><see langword="false"/> only if the user chose Cancel; otherwise <see langword="true"/>.</returns>
    public async Task<bool> ResolveUnsavedChangesAsync(bool allowCancel)
    {
        if (!Editor.IsDirty)
            return true;

        var contentName = Editor.FilePath is null ? "this content" : Path.GetFileName(Editor.FilePath);
        var decision = await _unsavedChangesDialog.ConfirmAsync(contentName, allowCancel).ConfigureAwait(true);

        switch (decision)
        {
            case UnsavedChangesDecision.Save:
                await Editor.SaveCommand.ExecuteAsync(null).ConfigureAwait(true);
                return true;
            case UnsavedChangesDecision.Discard:
                StatusMessage = "Unsaved changes were discarded.";
                return true;
            default:
                return false;
        }
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var path = await _folderPicker.PickFolderAsync("Open Kiln site").ConfigureAwait(true);
        if (path is null)
            return;

        await OpenPathAsync(path).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanCloseProject))]
    private async Task CloseProjectAsync()
    {
        if (!await ResolveUnsavedChangesAsync(allowCancel: true).ConfigureAwait(true))
            return;

        StopFullPreview();
        Explorer.Clear();
        Editor.Clear();
        CurrentProjectPath = null;
        CurrentProjectName = null;
        IsProjectOpen = false;
        CurrentDeploymentVariant = DeploymentVariant.None;
        StatusMessage = "Ready";
    }

    private bool CanCloseProject() => IsProjectOpen;

    [RelayCommand]
    private async Task SwitchRecentAsync(string path)
    {
        if (!await ResolveUnsavedChangesAsync(allowCancel: false).ConfigureAwait(true))
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
            Editor.Load(path, CurrentProjectPath, collection.Taxonomies);
            StatusMessage = $"Created {Path.GetFileName(path)}";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Failed to create page: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    [RelayCommand(CanExecute = nameof(CanUseCurrentProjectPath))]
    private void OpenInFileManager()
    {
        if (string.IsNullOrWhiteSpace(CurrentProjectPath))
            return;

        try
        {
            _folderRevealer.Reveal(CurrentProjectPath);
            StatusMessage = "Opened project folder.";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open folder: {ex.Message}";
        }
#pragma warning restore CA1031
    }

    [RelayCommand(CanExecute = nameof(CanUseCurrentProjectPath))]
    private async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrentProjectPath))
            return;

        await OpenPathAsync(CurrentProjectPath).ConfigureAwait(true);
    }

    private bool CanUseCurrentProjectPath() => IsProjectOpen && !IsBusy && !string.IsNullOrWhiteSpace(CurrentProjectPath);

    internal async Task OpenPathAsync(string path)
    {
        StopFullPreview();
        try
        {
            var project = await Task.Run(() => _projectService.Open(path)).ConfigureAwait(true);
            Explorer.Load(project);
            CurrentProjectPath = project.ProjectPath;
            CurrentProjectName = project.SiteTitle;
            StatusMessage = $"Opened {project.SiteTitle}";
            IsProjectOpen = true;
            _recentProjectsStore.Add(project.ProjectPath, project.SiteTitle);
            RefreshRecentProjects();

            var config = _deploymentConfigStore.Load(path);
            CurrentDeploymentVariant = config.Variant;
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
            // Save first so the dev-server serves what's actually on screen, not a stale file.
            if (Editor.IsDirty)
                await Editor.SaveCommand.ExecuteAsync(null).ConfigureAwait(true);

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

    [RelayCommand(CanExecute = nameof(CanBuild))]
    private async Task BuildAsync()
    {
        IsBusy = true;
        StatusMessage = ReleaseBuild ? "Building (release)..." : "Building (debug)...";

        try
        {
            var summary = await _buildService.BuildAsync(CurrentProjectPath!, ReleaseBuild).ConfigureAwait(true);
            StatusMessage = summary.Success
                ? $"Built {summary.RenderedFiles}/{summary.TotalFiles} files in {summary.DurationMs:F0} ms -> {summary.OutputDirectory}{FormatWarnings(summary.Warnings.Count)}"
                : $"Build failed: {GetFirstOrDefault(summary.Errors, "unknown error")}";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Build failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDeploy))]
    private async Task SetUpGitHubPagesAsync() => await SetUpDeploymentAsync(DeployTarget.GitHubPages).ConfigureAwait(true);

    [RelayCommand(CanExecute = nameof(CanDeploy))]
    private async Task SetUpAzureStaticWebAppsAsync() => await SetUpDeploymentAsync(DeployTarget.AzureStaticWebApps).ConfigureAwait(true);

    private bool CanBuild() => IsProjectOpen && !IsBusy;

    private bool CanDeploy() => IsProjectOpen && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanOpenSettings))]
    private async Task OpenSettingsAsync()
    {
        await _settingsDialog.ShowAsync(CurrentProjectPath!).ConfigureAwait(true);

        if (CurrentProjectPath is not null)
        {
            var config = _deploymentConfigStore.Load(CurrentProjectPath);
            CurrentDeploymentVariant = config.Variant;
        }
    }

    private bool CanOpenSettings() => IsProjectOpen;

    [RelayCommand]
    private void StopFullPreview()
    {
        _previewAutoSaveCts?.Cancel();
        _previewAutoSaveCts?.Dispose();
        _previewAutoSaveCts = null;

        _previewServer.StopServer();
        Preview.IsServing = false;
        Preview.ServeStatus = "Preview stopped";
        StatusMessage = Preview.ServeStatus;
        StartFullPreviewCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// While the full preview (real dev-server) is running, saves the currently-open document a
    /// short debounce window after it becomes dirty, so live-reload picks up further edits without
    /// requiring a manual Save click each time.
    /// </summary>
    private void OnEditorPropertyChangedForPreviewAutoSave(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(EditorViewModel.IsDirty) || !Editor.IsDirty || !Preview.IsServing)
            return;

        _previewAutoSaveCts?.Cancel();
        _previewAutoSaveCts?.Dispose();
        var cts = new CancellationTokenSource();
        _previewAutoSaveCts = cts;
        _ = DebouncedPreviewAutoSaveAsync(cts);
    }

    private async Task DebouncedPreviewAutoSaveAsync(CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(PreviewAutoSaveDebounceDelay, cts.Token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested || !Editor.IsDirty || !Preview.IsServing)
            return;

        await Editor.SaveCommand.ExecuteAsync(null).ConfigureAwait(true);
    }

    [RelayCommand(CanExecute = nameof(CanPublish))]
    private async Task PublishAsync()
    {
        IsBusy = true;
        StatusMessage = "Publishing...";

        try
        {
            var config = _deploymentConfigStore.Load(CurrentProjectPath!);
            var summary = await _publishService.PublishAsync(CurrentProjectPath!, config).ConfigureAwait(true);

            StatusMessage = summary.Success
                ? $"Published {summary.FileCount} files to {summary.Destination}"
                : $"Publish failed: {summary.Error}";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Publish failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ToggleDraftAsync(ContentEntryViewModel entry)
    {
        if (string.IsNullOrWhiteSpace(CurrentProjectPath))
            return;

        try
        {
            var newDraft = await Task.Run(() => _contentFrontmatterWriter.ToggleDraft(entry.SourcePath))
                .ConfigureAwait(true);

            Explorer.UpdateEntryDraft(entry.SourcePath, newDraft);
            StatusMessage = newDraft ? "Marked as draft." : "Unmarked draft.";
        }
        catch (ContentWriteException ex)
        {
            StatusMessage = ex.Message;
        }
    }

    /// <summary>
    /// Invoked by <see cref="EditorViewModel.PickAndPrepareAssetAsync"/> after it converts a flat
    /// content file into a page bundle (moved to a new folder/index.md). Re-reads the project so
    /// the explorer reflects the moved file, then re-selects the entry at its new path — this
    /// triggers the existing <see cref="OnExplorerPropertyChanged"/> reload path, which reloads the
    /// editor from the (unchanged) content at its new location.
    /// </summary>
    private async Task HandlePageBundleConvertedAsync(string newSourcePath)
    {
        await RefreshAsync().ConfigureAwait(true);

        // Compare via Path.GetFullPath (canonicalizes separators, "." segments, etc.) rather than
        // raw string equality: on Windows, a collection directory sourced from site.yaml (e.g. a
        // default of "content/{name}") can retain forward slashes that Path.Combine does not
        // normalize, so a SourcePath read from disk may mix '/' and '\' while newSourcePath (built
        // purely via Path.Combine/Path.GetDirectoryName, which DOES normalize) does not — raw `==`
        // would then never match even though both refer to the same file.
        var expectedFullPath = Path.GetFullPath(newSourcePath);
        var matchingEntry = Explorer.Collections
            .SelectMany(c => c.FilteredEntries)
            .FirstOrDefault(e => Path.GetFullPath(e.SourcePath) == expectedFullPath);

        if (matchingEntry is not null)
            Explorer.SelectedEntry = matchingEntry;
    }

    [RelayCommand]
    private async Task GenerateDeploymentConfigAsync()
    {
        if (CurrentProjectPath is null)
            return;

        IsBusy = true;

        try
        {
            var config = _deploymentConfigStore.Load(CurrentProjectPath);
            var target = config.Variant switch
            {
                DeploymentVariant.GitHubPages => DeployTarget.GitHubPages,
                DeploymentVariant.AzureStaticWebApps => DeployTarget.AzureStaticWebApps,
                _ => throw new InvalidOperationException($"No CI variant configured: {config.Variant}"),
            };

            var summary = await Task.Run(() => _deploymentService.SetUp(CurrentProjectPath, target)).ConfigureAwait(true);
            StatusMessage = $"Deployment configured ({FormatTarget(summary.Target)}): {string.Join(", ", summary.CreatedFiles)} - commit & push to deploy.";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Deployment config failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnCurrentDeploymentVariantChanged(DeploymentVariant value)
    {
        PublishCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(IsCiVariant));
        OnPropertyChanged(nameof(HasDeploymentVariant));
        OnPropertyChanged(nameof(NoDeploymentVariant));
    }

    private async Task SetUpDeploymentAsync(DeployTarget target)
    {
        IsBusy = true;
        StatusMessage = $"Setting up deployment ({FormatTarget(target)})...";

        try
        {
            var summary = await Task.Run(() => _deploymentService.SetUp(CurrentProjectPath!, target)).ConfigureAwait(true);
            StatusMessage = $"Deployment configured ({FormatTarget(summary.Target)}): {string.Join(", ", summary.CreatedFiles)} - commit & push to deploy.";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Deployment setup failed: {ex.Message}";
        }
#pragma warning restore CA1031
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatWarnings(int count) => count > 0 ? $" ({count} warning(s))" : string.Empty;

    private static string GetFirstOrDefault(IReadOnlyList<string> items, string fallback) => items.Count > 0 ? items[0] : fallback;

    private static string FormatTarget(DeployTarget target) => target switch
    {
        DeployTarget.GitHubPages => "GitHub Pages",
        DeployTarget.AzureStaticWebApps => "Azure Static Web Apps",
        _ => target.ToString(),
    };

    private void RefreshRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var rp in _recentProjectsStore.GetAll())
            RecentProjects.Add(new RecentProjectViewModel(
                rp.Name,
                rp.Path,
                new AsyncRelayCommand(() => OpenPathAsync(rp.Path))));
    }

    private sealed class NullFolderRevealer : IFolderRevealer
    {
        public void Reveal(string path)
        {
        }
    }

    private sealed class AutoDiscardUnsavedChangesDialog : IUnsavedChangesDialog
    {
        public Task<UnsavedChangesDecision> ConfirmAsync(string contentName, bool allowCancel) =>
            Task.FromResult(UnsavedChangesDecision.Discard);
    }
}
