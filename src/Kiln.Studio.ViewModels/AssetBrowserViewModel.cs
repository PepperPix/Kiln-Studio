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
    private const string LibraryAssetPrefix = "/assets/";

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

    /// <summary>
    /// Reverse index for site-library assets (<c>/assets/...</c> -&gt; content references).
    /// When null, delete/rename keep their legacy behavior.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<AssetContentReference>>? ReferenceIndex { get; set; }

    /// <summary>
    /// Controls visibility of the Insert action (default true for existing editor/flyout usages).
    /// </summary>
    public bool IsInsertVisible { get; init; } = true;

    /// <summary>
    /// Optional thumbnail cache. When provided, file entries in site scope get a generated preview.
    /// </summary>
    public IAssetThumbnailCache? ThumbnailCache { get; init; }

    /// <summary>
    /// Target thumbnail size in pixels (default 96, width or height boundary).
    /// </summary>
    public int ThumbnailSize { get; init; } = 96;

    /// <summary>
    /// Optional filter applied to every entry after refresh (e.g. "show only orphans").
    /// </summary>
    public Func<AssetLibraryEntry, bool>? EntryFilter { get; set; }

    /// <summary>
    /// Optional callback used when deleting a referenced library asset.
    /// Return true to continue, false to cancel.
    /// </summary>
    public Func<AssetLibraryEntry, IReadOnlyList<AssetContentReference>, Task<bool>>? ConfirmDeleteWithReferences { get; set; }

    /// <summary>
    /// Optional callback used before deleting any file (e.g. document-scoped reference checks).
    /// Return true to continue, false to cancel.
    /// </summary>
    public Func<AssetLibraryEntry, Task<bool>>? BeforeDelete { get; set; }

    /// <summary>
    /// Optional callback used to ask the host for a new file name.
    /// </summary>
    public Func<AssetLibraryEntry, Task<string?>>? PromptForRename { get; set; }

    /// <summary>
    /// Optional callback used to rewrite content references before a library rename.
    /// Return true to continue, false to cancel.
    /// </summary>
    public Func<string, string, IReadOnlyList<AssetContentReference>, Task<bool>>? RewriteReferencesOnRename { get; set; }

    /// <summary>
    /// Optional callback used before renaming any file (e.g. document-scoped body updates).
    /// Return true to continue, false to cancel.
    /// </summary>
    public Func<string, string, Task<bool>>? BeforeRename { get; set; }

    /// <summary>
    /// Optional callback invoked when the user clicks a "referenced by" content item.
    /// </summary>
    public Func<AssetContentReference, Task>? NavigateToReference { get; set; }

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

        var references = GetReferences(entry);
        if (references.Count > 0)
        {
            if (ConfirmDeleteWithReferences is null)
                return;

            var approved = await ConfirmDeleteWithReferences(entry, references).ConfigureAwait(true);
            if (!approved)
                return;
        }

        if (BeforeDelete is not null)
        {
            var canDelete = await BeforeDelete(entry).ConfigureAwait(true);
            if (!canDelete)
                return;
        }

        var absolutePath = ResolveAbsolutePath(entry.RelativePath);
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);

        await RefreshAsync().ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task RenameAsync(AssetLibraryEntry? entry)
    {
        if (entry is null || entry.IsFolder || PromptForRename is null)
            return;

        var requestedName = await PromptForRename(entry).ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(requestedName))
            return;

        var newName = requestedName.Trim();
        if (string.Equals(newName, entry.Name, StringComparison.Ordinal))
            return;

        var absolutePath = ResolveAbsolutePath(entry.RelativePath);
        if (!File.Exists(absolutePath))
            return;

        var directory = Path.GetDirectoryName(absolutePath)!;
        var newPath = Path.Combine(directory, newName);
        if (File.Exists(newPath))
            return;

        var oldRelativePath = entry.RelativePath;
        var newRelativePath = BuildRelativeSiblingPath(entry.RelativePath, newName);

        var references = GetReferences(entry);
        if (references.Count > 0)
        {
            if (RewriteReferencesOnRename is null)
                return;

            var oldWebPath = BuildLibraryWebPath(oldRelativePath);
            var newWebPath = BuildLibraryWebPath(newRelativePath);
            var rewritten = await RewriteReferencesOnRename(oldWebPath, newWebPath, references).ConfigureAwait(true);
            if (!rewritten)
                return;
        }

        if (BeforeRename is not null)
        {
            var canRename = await BeforeRename(oldRelativePath, newRelativePath).ConfigureAwait(true);
            if (!canRename)
                return;
        }

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

        var temp = new List<AssetLibraryEntry>();

        if (!string.IsNullOrEmpty(CurrentFolder))
        {
            temp.Add(new AssetLibraryEntry("..", true, ""));
        }

        if (_isDocumentScoped)
        {
            foreach (var directory in Directory.GetDirectories(currentAbsolute).OrderBy(Path.GetFileName))
            {
                var name = Path.GetFileName(directory);
                var relativePath = string.IsNullOrEmpty(CurrentFolder) ? name : $"{CurrentFolder}/{name}";
                temp.Add(MakeEntry(name, true, relativePath));
            }

            foreach (var file in Directory.GetFiles(currentAbsolute).OrderBy(Path.GetFileName))
            {
                var name = Path.GetFileName(file);
                var relativePath = string.IsNullOrEmpty(CurrentFolder) ? name : $"{CurrentFolder}/{name}";
                temp.Add(MakeEntry(name, false, relativePath, file));
            }
        }
        else
        {
            if (_projectPath is null)
                return Task.CompletedTask;

            var entries = _assetLibraryService.ListFolder(_projectPath, CurrentFolder);
            foreach (var entry in entries.OrderBy(e => (!e.IsFolder, e.Name)))
            {
                temp.Add(MakeEntry(entry.Name, entry.IsFolder, entry.RelativePath));
            }
        }

        var filter = EntryFilter;
        if (filter is not null)
        {
            temp = temp.Where(e => filter(e)).ToList();
        }

        foreach (var entry in temp)
        {
            Entries.Add(entry);
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task NavigateToReferenceAsync(AssetContentReference? reference)
    {
        if (reference is null)
            return;

        if (NavigateToReference is not null)
            await NavigateToReference(reference).ConfigureAwait(true);
    }

    private AssetLibraryEntry MakeEntry(string name, bool isFolder, string relativePath, string? absoluteFilePath = null)
    {
        var entry = new AssetLibraryEntry(name, isFolder, relativePath);

        if (!isFolder)
        {
            var references = GetReferences(entry);
            entry = entry with { References = references };

            if (ThumbnailCache is not null)
            {
                var path = absoluteFilePath ?? ResolveAbsolutePath(relativePath);
                var thumbnail = ThumbnailCache.GetOrCreateThumbnail(_projectPath, path, ThumbnailSize);
                entry = entry with { ThumbnailSource = thumbnail };
            }
        }

        return entry;
    }

    private string ResolveAbsolutePath(string relativePath)
    {
        var relative = relativePath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_rootFolderAbsolute, relative);
    }

    private IReadOnlyList<AssetContentReference> GetReferences(AssetLibraryEntry entry)
    {
        if (ReferenceIndex is null)
            return [];

        var key = BuildLibraryWebPath(entry.RelativePath);
        if (!ReferenceIndex.TryGetValue(key, out var references) || references.Count == 0)
            return [];

        return references;
    }

    private static string BuildLibraryWebPath(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        return LibraryAssetPrefix + normalized;
    }

    private static string BuildRelativeSiblingPath(string currentRelativePath, string newName)
    {
        var lastSlash = currentRelativePath.LastIndexOf('/');
        return lastSlash < 0 ? newName : $"{currentRelativePath[..lastSlash]}/{newName}";
    }
}
