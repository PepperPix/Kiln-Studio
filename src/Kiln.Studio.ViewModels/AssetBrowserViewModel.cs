namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

/// <summary>
/// Reusable, Avalonia-free view model for browsing and managing assets either in the site-wide
/// static/ library (site scope) or in the current content item's page bundle (document scope).
/// Hosted inside the editor's Assets tab (document scope) and inside the Asset toolbar button's
/// Flyout (site scope) — both share this same view model and the companion
/// <c>AssetBrowserView</c> (Kiln.Studio project).
/// </summary>
public sealed partial class AssetBrowserViewModel : ViewModelBase
{
    private readonly IAssetLibraryService _assetLibraryService;
    private readonly IFilePicker _filePicker;
    private readonly string? _projectPath;
    private readonly string _rootFolderAbsolute;
    private readonly bool _isDocumentScoped;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Breadcrumb))]
    private string _currentFolder = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModeButtonText))]
    [NotifyPropertyChangedFor(nameof(IsEmptyStateVisible))]
    private bool _isUploadMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUpload))]
    private string? _chosenFilePath;

    [ObservableProperty]
    private AssetPickerDestination _uploadDestination = AssetPickerDestination.PageBundle;

    [ObservableProperty]
    private string _newFolderName = "";

    /// <summary>
    /// Callback invoked when the user chooses an existing asset (double-click/insert button) or
    /// completes an upload. The host (usually <see cref="EditorViewModel"/>) converts the result
    /// into a Markdown snippet and inserts it at the editor caret.
    /// </summary>
    public Func<AssetPickerResult, Task>? AssetChosen { get; set; }

    public AssetBrowserViewModel(
        IAssetLibraryService assetLibraryService,
        IFilePicker filePicker,
        string? projectPath,
        string rootFolderAbsolute,
        bool isDocumentScoped)
    {
        _assetLibraryService = assetLibraryService;
        _filePicker = filePicker;
        _projectPath = projectPath;
        _rootFolderAbsolute = rootFolderAbsolute;
        _isDocumentScoped = isDocumentScoped;

        if (!isDocumentScoped)
        {
            // In site scope uploads default to the library unless a page bundle is available.
            UploadDestination = AssetPickerDestination.Library;
        }

        Entries.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmptyStateVisible));

        _ = RefreshAsync();
    }

    public bool IsDocumentScoped => _isDocumentScoped;

    public bool IsSiteScoped => !_isDocumentScoped;

    public bool CanChooseUploadDestination => !_isDocumentScoped;

    public bool CanUpload => !string.IsNullOrEmpty(ChosenFilePath);

    public ObservableCollection<AssetLibraryEntry> Entries { get; } = [];

    public bool IsEmptyStateVisible => !IsUploadMode && Entries.Count == 0;

    public string Breadcrumb => string.IsNullOrEmpty(CurrentFolder)
        ? RootDisplayName
        : $"{RootDisplayName}/{CurrentFolder}";

    private string RootDisplayName => _isDocumentScoped ? "Bundle" : "static";

    /// <summary>
    /// Navigates into a folder entry. Files are not navigable.
    /// </summary>
    [RelayCommand]
    private void NavigateInto(AssetLibraryEntry? entry)
    {
        if (entry?.IsFolder != true)
            return;

        if (string.IsNullOrEmpty(entry.RelativePath))
        {
            NavigateUp();
            return;
        }

        CurrentFolder = entry.RelativePath;
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void NavigateUp()
    {
        if (string.IsNullOrEmpty(CurrentFolder))
            return;

        var lastSlash = CurrentFolder.LastIndexOf('/');
        CurrentFolder = lastSlash < 0 ? "" : CurrentFolder[..lastSlash];
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task CreateFolderAsync()
    {
        var name = NewFolderName;
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (_isDocumentScoped)
        {
            var targetDir = Path.Combine(_rootFolderAbsolute, CurrentFolder, name);
            Directory.CreateDirectory(targetDir);
        }
        else
        {
            if (_projectPath is null)
                return;

            _assetLibraryService.CreateFolder(_projectPath, CurrentFolder, name);
        }

        NewFolderName = "";
        await RefreshAsync().ConfigureAwait(true);
    }

    public string ModeButtonText => IsUploadMode ? "Switch to Browse" : "Switch to Upload";

    [RelayCommand]
    private void ToggleMode() => IsUploadMode = !IsUploadMode;

    [RelayCommand]
    private async Task ChooseFileAsync()
    {
        var picked = await _filePicker.PickFileAsync("Select file to upload").ConfigureAwait(true);
        if (picked is null)
            return;

        ChosenFilePath = picked;
    }

    [RelayCommand]
    private async Task UploadAsync()
    {
        var sourcePath = ChosenFilePath;
        if (string.IsNullOrEmpty(sourcePath))
            return;

        AssetPickerResult result;
        if (_isDocumentScoped || UploadDestination == AssetPickerDestination.PageBundle)
        {
            result = new AssetPickerResult(AssetPickerDestination.PageBundle, sourcePath);
        }
        else
        {
            if (_projectPath is null)
                return;

            var relativePath = _assetLibraryService.Upload(_projectPath, CurrentFolder, sourcePath);
            result = new AssetPickerResult(AssetPickerDestination.Library, relativePath);
        }

        ChosenFilePath = null;
        IsUploadMode = false;
        await RefreshAsync().ConfigureAwait(true);
        if (AssetChosen is not null)
            await AssetChosen(result).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task DeleteAsync(AssetLibraryEntry? entry)
    {
        if (entry is null || entry.IsFolder)
            return;

        var absolutePath = ResolveAbsolutePath(entry.RelativePath);
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);

        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RenameAsync((AssetLibraryEntry Entry, string NewName)? parameter)
    {
        if (parameter is null || string.IsNullOrWhiteSpace(parameter.Value.NewName))
            return;

        var entry = parameter.Value.Entry;
        var newName = parameter.Value.NewName.Trim();
        if (entry.IsFolder)
            return;

        var absolutePath = ResolveAbsolutePath(entry.RelativePath);
        if (!File.Exists(absolutePath))
            return;

        var directory = Path.GetDirectoryName(absolutePath)!;
        var newPath = Path.Combine(directory, newName);
        if (File.Exists(newPath))
            return;

        File.Move(absolutePath, newPath);
        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task InsertAsync(AssetLibraryEntry? entry)
    {
        if (entry?.IsFolder == true || entry is null)
            return;

        AssetPickerResult result;
        if (_isDocumentScoped)
        {
            // Existing asset inside the current page bundle: relative reference, no re-upload.
            result = new AssetPickerResult(AssetPickerDestination.PageBundleExisting, entry.RelativePath);
        }
        else
        {
            result = new AssetPickerResult(AssetPickerDestination.Library, entry.RelativePath);
        }

        if (AssetChosen is not null)
            await AssetChosen(result).ConfigureAwait(true);
    }

    public Task RefreshAsync()
    {
        Entries.Clear();

        if (!Directory.Exists(_rootFolderAbsolute))
        {
            // Flat file without a bundle: show empty state. Upload will create the bundle via
            // the host's page-bundle conversion path.
            return Task.CompletedTask;
        }

        var currentAbsolute = Path.Combine(_rootFolderAbsolute, CurrentFolder.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(currentAbsolute))
            return Task.CompletedTask;

        if (!string.IsNullOrEmpty(CurrentFolder))
        {
            Entries.Add(new AssetLibraryEntry("..", true, ""));
        }

        if (_isDocumentScoped)
        {
            foreach (var directory in Directory.GetDirectories(currentAbsolute).OrderBy(Path.GetFileName))
            {
                var name = Path.GetFileName(directory);
                var relativePath = string.IsNullOrEmpty(CurrentFolder) ? name : $"{CurrentFolder}/{name}";
                Entries.Add(new AssetLibraryEntry(name, true, relativePath));
            }

            foreach (var file in Directory.GetFiles(currentAbsolute).OrderBy(Path.GetFileName))
            {
                var name = Path.GetFileName(file);
                var relativePath = string.IsNullOrEmpty(CurrentFolder) ? name : $"{CurrentFolder}/{name}";
                Entries.Add(new AssetLibraryEntry(name, false, relativePath));
            }
        }
        else
        {
            if (_projectPath is null)
                return Task.CompletedTask;

            var entries = _assetLibraryService.ListFolder(_projectPath, CurrentFolder);
            foreach (var entry in entries.OrderBy(e => (!e.IsFolder, e.Name)))
            {
                Entries.Add(entry);
            }
        }

        return Task.CompletedTask;
    }

    private string ResolveAbsolutePath(string relativePath)
    {
        var relative = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_rootFolderAbsolute, relative);
    }
}
