namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Models;
using Kiln.Services;
using Kiln.Studio.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed partial class AssetManagerViewModel : ViewModelBase
{
    private readonly EngineHost _engineHost;
    private readonly IAssetLibraryService _assetLibraryService;
    private readonly IFilePicker _filePicker;
    private readonly IInputDialog _inputDialog;
    private readonly IContentService _contentService;
    private readonly IContentBodyReferenceRewriter _referenceRewriter;
    private readonly IAssetThumbnailCache _thumbnailCache;

    private string? _projectPath;
    private readonly Dictionary<string, IReadOnlyList<AssetContentReference>> _referenceIndex = new(StringComparer.Ordinal);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLibraryMode))]
    [NotifyPropertyChangedFor(nameof(IsByContentMode))]
    private AssetManagerMode _mode = AssetManagerMode.Library;

    [ObservableProperty]
    private bool _showOnlyOrphans;

    [ObservableProperty]
    private AssetBrowserViewModel? _library;

    public ObservableCollection<AssetManagerContentGroupViewModel> ContentGroups { get; } = [];

    public bool IsLibraryMode => Mode == AssetManagerMode.Library;

    public bool IsByContentMode => Mode == AssetManagerMode.ByContent;

    public AssetManagerViewModel(
        EngineHost engineHost,
        IAssetLibraryService assetLibraryService,
        IFilePicker filePicker,
        IInputDialog inputDialog,
        IContentService contentService,
        IContentBodyReferenceRewriter referenceRewriter,
        IAssetThumbnailCache thumbnailCache)
    {
        _engineHost = engineHost;
        _assetLibraryService = assetLibraryService;
        _filePicker = filePicker;
        _inputDialog = inputDialog;
        _contentService = contentService;
        _referenceRewriter = referenceRewriter;
        _thumbnailCache = thumbnailCache;
    }

    public void LoadProject(string projectPath)
    {
        _projectPath = projectPath;
        Rebuild();
    }

    public void ClearProject()
    {
        _projectPath = null;
        _referenceIndex.Clear();
        Library = null;
        ContentGroups.Clear();
    }

    [RelayCommand]
    private void Refresh()
    {
        if (_projectPath is null)
            return;

        Rebuild();
    }

    [RelayCommand]
    private void ShowLibrary() => Mode = AssetManagerMode.Library;

    [RelayCommand]
    private void ShowByContent() => Mode = AssetManagerMode.ByContent;

    partial void OnModeChanged(AssetManagerMode value)
    {
        if (value == AssetManagerMode.Library && Library is null && _projectPath is not null)
            Rebuild();
    }

    private void Rebuild()
    {
        if (_projectPath is null)
            return;

        _referenceIndex.Clear();

        var (allItems, engineReferenceIndex) = LoadAllContentItems(_projectPath);

        BuildReferenceIndex(engineReferenceIndex);

        Library = BuildLibraryBrowser(_projectPath);

        ContentGroups.Clear();
        foreach (var group in BuildContentGroups(_projectPath, allItems))
            ContentGroups.Add(group);
    }

    private AssetBrowserViewModel BuildLibraryBrowser(string projectPath)
    {
        var browser = new AssetBrowserViewModel(
            _assetLibraryService,
            _filePicker,
            projectPath,
            Path.Combine(projectPath, "static"),
            isDocumentScoped: false)
        {
            IsInsertVisible = false,
            ReferenceIndex = _referenceIndex,
            ThumbnailCache = _thumbnailCache,
            ThumbnailSize = 96,
            EntryFilter = ShowOnlyOrphans ? IsOrphan : null,
            PromptForRename = PromptForRenameAsync,
            ConfirmDeleteWithReferences = ConfirmDeleteWithReferencesAsync,
            RewriteReferencesOnRename = RewriteLibraryReferencesOnRenameAsync,
            NavigateToReference = NavigateToReferenceAsync,
            BeforeRename = (_, _) => Task.FromResult(true),
            BeforeDelete = _ => Task.FromResult(true)
        };

        _ = browser.RefreshAsync();
        return browser;
    }

    private IEnumerable<AssetManagerContentGroupViewModel> BuildContentGroups(string projectPath, IReadOnlyList<ContentItem> allItems)
    {
        foreach (var item in allItems)
        {
            if (string.IsNullOrWhiteSpace(item.AssetDirectory) || !Directory.Exists(item.AssetDirectory))
                continue;

            var browser = new AssetBrowserViewModel(
                _assetLibraryService,
                _filePicker,
                projectPath,
                item.AssetDirectory,
                isDocumentScoped: true)
            {
                IsInsertVisible = false,
                ThumbnailCache = _thumbnailCache,
                ThumbnailSize = 96,
                PromptForRename = PromptForRenameAsync,
                BeforeDelete = entry => BeforeDeleteBundleAssetAsync(item.SourcePath, entry),
                BeforeRename = (oldPath, newPath) => BeforeRenameBundleAssetAsync(item.SourcePath, oldPath, newPath)
            };

            _ = browser.RefreshAsync();
            yield return new AssetManagerContentGroupViewModel(item.Title, browser);
        }
    }

    private (List<ContentItem> Items, IReadOnlyDictionary<string, IReadOnlyList<ContentItemRef>> Index) LoadAllContentItems(string projectPath)
    {
        using var provider = _engineHost.CreateProvider(projectPath);
        var loader = provider.GetRequiredService<ISiteConfigLoader>();
        var reader = provider.GetRequiredService<IContentReader>();
        var indexBuilder = provider.GetRequiredService<IAssetReferenceIndexBuilder>();
        var config = loader.Load(projectPath);

        var items = config.Collections
            .SelectMany(kv => reader.ReadCollection(kv.Value, projectPath))
            .ToList();

        var index = indexBuilder.Build(items);
        return (items, index);
    }

    private void BuildReferenceIndex(IReadOnlyDictionary<string, IReadOnlyList<ContentItemRef>> engineIndex)
    {
        _referenceIndex.Clear();
        foreach (var (key, refs) in engineIndex)
        {
            _referenceIndex[key] = refs.Select(r => new AssetContentReference(r.SourcePath, r.Title)).ToList();
        }
    }

    private bool IsOrphan(AssetLibraryEntry entry)
    {
        if (entry.IsFolder)
            return true;

        var key = "/assets/" + entry.RelativePath.TrimStart('/');
        return !_referenceIndex.TryGetValue(key, out var refs) || refs.Count == 0;
    }

    partial void OnShowOnlyOrphansChanged(bool value)
    {
        if (Library is null)
            return;

        Library.EntryFilter = value ? IsOrphan : null;
        _ = Library.RefreshAsync();
    }

    private Task NavigateToReferenceAsync(AssetContentReference reference)
    {
        NavigateToContentItem?.Invoke(reference.SourcePath);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Callback used by library asset browsers to jump from a "referenced by" item into the
    /// content editor. Bound by the host (ShellViewModel).
    /// </summary>
    public Action<string>? NavigateToContentItem { get; set; }

    private async Task<string?> PromptForRenameAsync(AssetLibraryEntry entry)
    {
        var value = await _inputDialog.PromptAsync("Rename asset", $"New name for '{entry.Name}':").ConfigureAwait(true);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private async Task<bool> ConfirmDeleteWithReferencesAsync(AssetLibraryEntry entry, IReadOnlyList<AssetContentReference> references)
    {
        var refsText = string.Join(", ", references.Select(r => r.Title));
        var confirmation = await _inputDialog
            .PromptAsync("Delete referenced asset", $"'{entry.Name}' is referenced by: {refsText}. Type DELETE to confirm.")
            .ConfigureAwait(true);

        return string.Equals(confirmation, "DELETE", StringComparison.Ordinal);
    }

    private Task<bool> RewriteLibraryReferencesOnRenameAsync(string oldPath, string newPath, IReadOnlyList<AssetContentReference> references)
    {
        var originals = new Dictionary<string, ContentDocument>(StringComparer.Ordinal);
        var updates = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var reference in references)
        {
            var document = _contentService.Load(reference.SourcePath);
            originals[reference.SourcePath] = document;
            updates[reference.SourcePath] = _referenceRewriter.Rewrite(document.Body, oldPath, newPath);
        }

        try
        {
            foreach (var (sourcePath, body) in updates)
            {
                var original = originals[sourcePath];
                _contentService.Save(sourcePath, original.FrontMatter, body);
            }
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            foreach (var (sourcePath, original) in originals)
            {
                _contentService.Save(sourcePath, original.FrontMatter, original.Body);
            }

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private Task<bool> BeforeDeleteBundleAssetAsync(string sourcePath, AssetLibraryEntry entry)
    {
        var path = "./" + entry.RelativePath.TrimStart('/');
        var document = _contentService.Load(sourcePath);

        // bundle assets affect only their owning content file; delete is blocked when still referenced
        return Task.FromResult(!document.Body.Contains(path, StringComparison.Ordinal));
    }

    private Task<bool> BeforeRenameBundleAssetAsync(string sourcePath, string oldRelativePath, string newRelativePath)
    {
        var oldPath = "./" + oldRelativePath.TrimStart('/');
        var newPath = "./" + newRelativePath.TrimStart('/');
        var document = _contentService.Load(sourcePath);
        var rewritten = _referenceRewriter.Rewrite(document.Body, oldPath, newPath);
        _contentService.Save(sourcePath, document.FrontMatter, rewritten);
        return Task.FromResult(true);
    }
}

public sealed class AssetManagerContentGroupViewModel : ViewModelBase
{
    public string Title { get; }

    public AssetBrowserViewModel Browser { get; }

    public AssetManagerContentGroupViewModel(string title, AssetBrowserViewModel browser)
    {
        Title = title;
        Browser = browser;
    }
}
