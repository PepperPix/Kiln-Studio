namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Models;
using Kiln.Services;
using Kiln.Studio.Services;
using Microsoft.Extensions.DependencyInjection;

public partial class EditorViewModel : ViewModelBase
{
    private static readonly string[] ScalarOwnedKeys = ["title", "date", "description"];
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"];
    private const string AssetsUrlPrefix = "/assets/";

    private static readonly Regex ImageMarkdownRegex = new(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);
    private static readonly Regex AssetReferenceRegex = new(@"!?\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);
    private const int WordsPerMinute = 200;

    private readonly IContentService _contentService;
    private readonly IContentFrontmatterWriter _frontmatterWriter;
    private readonly ITaxonomyValueCache _taxonomyValueCache;
    private readonly IAssetLibraryService _assetLibraryService;
    private readonly IPageBundleService _pageBundleService;
    private readonly IImageDimensionReader _imageDimensionReader;
    private readonly IFilePicker _filePicker;
    private readonly EngineHost _engineHost;
    private Func<string, Task>? _onPageBundleConverted;
    private bool _suppressDirty;
    private string? _projectPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PreviewMarkdown), nameof(CreatedAt), nameof(ModifiedAt))]
    private string? _filePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _hasDocument;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    /// <summary>
    /// Toggles the right-hand editor panel (Details/Stats/Preview/Assets tabs). Defaults to
    /// expanded so the structured Details tab is visible when an item loads.
    /// </summary>
    [ObservableProperty]
    private bool _isRightPanelExpanded = true;

    [ObservableProperty]
    private string _frontMatter = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(PreviewMarkdown),
        nameof(WordCount),
        nameof(CharacterCount),
        nameof(ReadingTimeMinutes),
        nameof(ReferencedAssetCount))]
    private string _body = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private DateTimeOffset? _date;

    [ObservableProperty]
    private string _description = "";

    [ObservableProperty]
    private string? _lastAssetFeedback;

    public ObservableCollection<TaxonomyFieldViewModel> TaxonomyFields { get; } = [];

    /// <summary>
    /// Asset browser for the Assets tab, scoped to the currently open document's page bundle.
    /// Recreated every time <see cref="Load"/> runs because the underlying directory changes.
    /// </summary>
    public AssetBrowserViewModel? DocumentAssets { get; private set; }

    /// <summary>
    /// Asset browser for the site-wide library Flyout opened from the Markdown toolbar. Recreated
    /// every time <see cref="Load"/> runs because the project path may change.
    /// </summary>
    public AssetBrowserViewModel? FlyoutAssets { get; private set; }

    /// <summary>
    /// Callback invoked whenever an asset browser (tab or flyout) has produced a Markdown snippet
    /// that should be inserted at the editor caret. The view subscribes to this and performs the
    /// actual TextEditor insertion so the ViewModel stays Avalonia-free.
    /// </summary>
    public Action<string>? AssetSnippetReady { get; set; }

#pragma warning disable S107 // Constructor mirrors the existing dependency pattern; collapsing services would break testability.
    public EditorViewModel(
        IContentService contentService,
        IContentFrontmatterWriter? frontmatterWriter = null,
        ITaxonomyValueCache? taxonomyValueCache = null,
        IAssetLibraryService? assetLibraryService = null,
        IPageBundleService? pageBundleService = null,
        IImageDimensionReader? imageDimensionReader = null,
        IFilePicker? filePicker = null,
        EngineHost? engineHost = null)
    {
        _contentService = contentService;
        _frontmatterWriter = frontmatterWriter ?? new ContentFrontmatterWriter();
        _taxonomyValueCache = taxonomyValueCache ?? new TaxonomyValueCache();
        _assetLibraryService = assetLibraryService ?? new AssetLibraryService();
        _pageBundleService = pageBundleService ?? new PageBundleService();
        _imageDimensionReader = imageDimensionReader ?? new NullImageDimensionReader();
        _filePicker = filePicker ?? new NullFilePicker();
        _engineHost = engineHost ?? new EngineHost();
    }
#pragma warning restore S107

    /// <summary>
    /// Registers the callback invoked after <see cref="PrepareAssetSnippetAsync"/> converts a flat
    /// content file into a page bundle, so the owner (ShellViewModel) can resynchronize the
    /// explorer with the moved file (analogous to <c>ProjectExplorerViewModel.SetDraftToggleHandler</c>).
    /// </summary>
    public void SetPageBundleConvertedHandler(Func<string, Task> handler) => _onPageBundleConverted = handler;

    // ── Stats sub-tab (ADR-054/PLAN-072: word count/characters/reading time/referenced assets, plus
    //    file-system created/modified dates) - purely informative, computed on demand from Body/FilePath. ──

    public int WordCount => string.IsNullOrWhiteSpace(Body)
        ? 0
        : Body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    public int CharacterCount => Body.Length;

    public int ReadingTimeMinutes => Math.Max(1, (int)Math.Ceiling(WordCount / (double)WordsPerMinute));

    /// <summary>
    /// Number of Markdown image/link references in <see cref="Body"/> that point at a Kiln asset
    /// (either the site asset library, "/assets/...", or a page-bundle-relative file, "./..." -
    /// the two schemes produced by <see cref="PrepareAssetSnippetAsync"/>). Deliberately a simple
    /// forward count over the item's own body, NOT the project-wide reverse reference index
    /// (that is a separate, larger concern - see PLAN-071's <c>IAssetReferenceIndexBuilder</c>).
    /// </summary>
    public int ReferencedAssetCount
    {
        get
        {
            var count = 0;
            foreach (Match match in AssetReferenceRegex.Matches(Body))
            {
                var path = match.Groups[2].Value;
                if (path.StartsWith(AssetsUrlPrefix, StringComparison.Ordinal) ||
                    path.StartsWith("./", StringComparison.Ordinal))
                    count++;
            }

            return count;
        }
    }

    public DateTime? CreatedAt => FilePath is not null && File.Exists(FilePath) ? File.GetCreationTime(FilePath) : null;

    public DateTime? ModifiedAt => FilePath is not null && File.Exists(FilePath) ? File.GetLastWriteTime(FilePath) : null;

    public void Load(string filePath, string? projectPath = null, IReadOnlyList<string>? taxonomyNames = null)
    {
        _suppressDirty = true;
        try
        {
            var doc = _contentService.Load(filePath);
            var names = taxonomyNames ?? [];
            FilePath = filePath;
            FrontMatter = _frontmatterWriter.RemoveOwnedKeys(doc.FrontMatter, BuildOwnedKeys(names));
            Body = doc.Body;
            Title = _frontmatterWriter.GetScalarValue(filePath, "title") ?? "";
            Date = ParseDate(_frontmatterWriter.GetScalarValue(filePath, "date"));
            Description = _frontmatterWriter.GetScalarValue(filePath, "description") ?? "";
            HasDocument = true;
            IsDirty = false;
            _projectPath = projectPath;
            LoadTaxonomyFields(filePath, projectPath, names);
            RebuildAssetBrowsers();
        }
        finally
        {
            _suppressDirty = false;
        }
    }

    private static List<string> BuildOwnedKeys(IReadOnlyList<string> taxonomyNames)
    {
        var keys = new List<string>(ScalarOwnedKeys);
        keys.AddRange(taxonomyNames);
        return keys;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
    }

    private void LoadTaxonomyFields(string filePath, string? projectPath, IReadOnlyList<string> taxonomyNames)
    {
        TaxonomyFields.Clear();
        foreach (var name in taxonomyNames)
        {
            var field = new TaxonomyFieldViewModel(name);
            foreach (var value in _frontmatterWriter.GetTaxonomyValues(filePath, name))
                field.Values.Add(value);

            if (projectPath is not null)
            {
                foreach (var suggestion in _taxonomyValueCache.GetSuggestions(projectPath, name))
                    field.Suggestions.Add(suggestion);
            }

            // Attach only after the initial population above, so restoring existing values on
            // load never marks the freshly-opened document dirty.
            field.Values.CollectionChanged += OnTaxonomyFieldValuesChanged;
            TaxonomyFields.Add(field);
        }
    }

    private void OnTaxonomyFieldValuesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressDirty)
            IsDirty = true;
    }

    public void Clear()
    {
        _suppressDirty = true;
        try
        {
            FilePath = null;
            FrontMatter = "";
            Body = "";
            Title = "";
            Date = null;
            Description = "";
            HasDocument = false;
            IsDirty = false;
            _projectPath = null;
            TaxonomyFields.Clear();
            DocumentAssets = null;
            FlyoutAssets = null;
        }
        finally
        {
            _suppressDirty = false;
        }
    }

    partial void OnFrontMatterChanged(string value)
    {
        if (!_suppressDirty)
            IsDirty = true;
    }

    partial void OnBodyChanged(string value)
    {
        if (!_suppressDirty)
            IsDirty = true;
    }

    partial void OnTitleChanged(string value)
    {
        if (!_suppressDirty)
            IsDirty = true;
    }

    partial void OnDateChanged(DateTimeOffset? value)
    {
        if (!_suppressDirty)
            IsDirty = true;
    }

    partial void OnDescriptionChanged(string value)
    {
        if (!_suppressDirty)
            IsDirty = true;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var filePath = FilePath!;
        var frontMatter = FrontMatter;
        var body = Body;
        var title = Title;
        var date = Date;
        var description = Description;
        var projectPath = _projectPath;
        var taxonomySnapshot = TaxonomyFields
            .Select(f => (f.Name, Values: (IReadOnlyList<string>)f.Values.ToList()))
            .ToList();

        await Task.Run(() =>
        {
            _contentService.Save(filePath, frontMatter, body);
            _frontmatterWriter.SetScalarValue(filePath, "title", title);
            _frontmatterWriter.SetScalarValue(filePath, "date", date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            _frontmatterWriter.SetScalarValue(filePath, "description", description);
            foreach (var (name, values) in taxonomySnapshot)
            {
                _frontmatterWriter.SetTaxonomyValues(filePath, name, values);
                if (projectPath is not null && values.Count > 0)
                    _taxonomyValueCache.AddValues(projectPath, name, values);
            }
        }).ConfigureAwait(true);

        IsDirty = false;
    }

    private bool CanSave() => HasDocument && IsDirty && FilePath is not null;

    /// <summary>
    /// Converts an already-made asset picker decision into the finished Markdown snippet to insert
    /// at the caret, or <see langword="null"/> if the result is null. Deliberately does not touch
    /// the body editor control or caret position itself — inserting the returned snippet is the
    /// view's responsibility (see EditorView.axaml.cs).
    /// </summary>
    public async Task<string?> PrepareAssetSnippetAsync(AssetPickerResult? pickerResult)
    {
        if (pickerResult is null)
            return null;

        string markdownPath;
        string fileName;
        string? physicalPath;

        if (pickerResult.Destination == AssetPickerDestination.Library)
        {
            var normalized = pickerResult.Path.Replace('\\', '/');
            markdownPath = $"/assets/{normalized}";
            fileName = Path.GetFileName(normalized);
            // AssetLibraryService.CombineRelative always builds forward-slash-joined relative
            // paths by design (canonical, platform-independent — see AssetLibraryService.cs), NOT
            // native separators. Path.Combine only normalizes the separator BETWEEN its arguments,
            // never inside one, so combining with pickerResult.Path as-is leaves a mixed
            // "...\static\images/photo.png" path on Windows (confirmed via CI: windows-latest).
            // Convert to the platform separator explicitly before combining.
            var nativeRelativePath = normalized.Replace('/', Path.DirectorySeparatorChar);
            physicalPath = _projectPath is not null ? Path.Combine(_projectPath, "static", nativeRelativePath) : null;
        }
        else if (pickerResult.Destination == AssetPickerDestination.PageBundleExisting)
        {
            // Asset already lives in the current page bundle; just reference it relatively.
            var normalized = pickerResult.Path.Replace('\\', '/');
            fileName = Path.GetFileName(normalized);
            markdownPath = $"./{normalized}";
            physicalPath = FilePath is not null
                ? Path.Combine(Path.GetDirectoryName(FilePath)!, normalized.Replace('/', Path.DirectorySeparatorChar))
                : null;
        }
        else
        {
            if (IsDirty)
                await SaveAsync().ConfigureAwait(true);

            var uploadResult = _pageBundleService.UploadAsset(FilePath!, pickerResult.Path);
            if (uploadResult.WasConverted)
                await (_onPageBundleConverted?.Invoke(uploadResult.NewSourcePath) ?? Task.CompletedTask).ConfigureAwait(true);

            fileName = uploadResult.RelativeAssetFileName;
            markdownPath = $"./{fileName}";
            physicalPath = Path.Combine(Path.GetDirectoryName(uploadResult.NewSourcePath)!, uploadResult.RelativeAssetFileName);
        }

        var isImage = ImageExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
        LastAssetFeedback = isImage ? BuildAssetFeedback(physicalPath) : null;
        return isImage ? $"![]({markdownPath})" : $"[{fileName}]({markdownPath})";
    }

    /// <summary>
    /// Rein informative Rückmeldung nach einem Bild-Upload/-Pick: Maße, Dateigröße, und eine
    /// deterministische Skalierungs-Aussage basierend auf <c>site.yaml</c>s <c>images.*</c>
    /// (ADR-051). Gibt <see langword="null"/> zurück, wenn die Datei nicht existiert oder ihre
    /// Maße nicht gelesen werden können — nie einen Fallback-Text.
    /// </summary>
    private string? BuildAssetFeedback(string? physicalPath)
    {
        if (physicalPath is null || !File.Exists(physicalPath))
            return null;

        var dimensions = _imageDimensionReader.TryReadDimensions(physicalPath);
        if (dimensions is null)
            return null;

        var (width, height) = dimensions.Value;
        var fileSizeBytes = new FileInfo(physicalPath).Length;
        var images = LoadImageOptions();
        return FormatAssetFeedback(width, height, fileSizeBytes, images);
    }

    private ImageOptions LoadImageOptions()
    {
        if (_projectPath is null)
            return new ImageOptions();

        using var provider = _engineHost.CreateProvider(_projectPath);
        var loader = provider.GetRequiredService<ISiteConfigLoader>();
        return loader.Load(_projectPath).Images;
    }

    private static string FormatAssetFeedback(int width, int height, long fileSizeBytes, ImageOptions images)
    {
        var sizeFormatted = FormatFileSize(fileSizeBytes);

        if (!images.Enabled)
            return $"{width}\u00d7{height}px, {sizeFormatted} — Bild-Optimierung ist für dieses Projekt deaktiviert.";

        if (width <= images.MaxWidth)
            return $"{width}\u00d7{height}px, {sizeFormatted} — bleibt beim Build in Originalgröße (Limit: {images.MaxWidth}px).";

        return $"{width}\u00d7{height}px, {sizeFormatted} — wird beim Build auf {images.MaxWidth}px Breite skaliert.";
    }

    private static string FormatFileSize(long bytes)
    {
        const double kb = 1024;
        const double mb = 1024 * 1024;

        if (bytes >= mb)
        {
            var value = (bytes / mb).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            return $"{value} MB";
        }

        var kbValue = (long)Math.Round(bytes / kb, MidpointRounding.AwayFromZero);
        return $"{kbValue} KB";
    }

    /// <summary>
    /// <see cref="Body"/> with image references rewritten to fully-qualified <c>file://</c> URIs,
    /// for binding to Studio's in-app Markdown.Avalonia quick-preview pane (<c>MarkdownScrollViewer</c>
    /// in ShellWindow.axaml). Bind THIS, not <see cref="Body"/>, to that preview.
    ///
    /// Why this is necessary rather than just setting MarkdownScrollViewer.AssetPathRoot: its
    /// DefaultPathResolver resolves relative paths via <c>Path.Combine(AssetPathRoot, path)</c> —
    /// but .NET's Path.Combine silently DISCARDS the first argument whenever the second is rooted
    /// (starts with '/'), which our site-library markdown scheme always does ("/assets/..."). A
    /// single AssetPathRoot value also cannot handle both of our two path kinds at once (page-bundle
    /// "./x.png", resolved relative to the open item's own directory, vs. site-library "/assets/x.png",
    /// resolved relative to the project's static/ folder). Pre-resolving to absolute file:// URIs here
    /// sidesteps both problems: DefaultPathResolver's very first check (Uri.TryCreate(..., Absolute))
    /// succeeds immediately and never reaches the buggy AssetPathRoot/Path.Combine branch. Using
    /// Uri.AbsoluteUri (rather than string concatenation) also correctly percent-encodes spaces/
    /// special characters in file names, which a literal "file://" + raw path would not.
    /// </summary>
    public string PreviewMarkdown => RewriteImagePathsForPreview(Body);

    private string RewriteImagePathsForPreview(string body)
    {
        if (FilePath is null)
            return body;

        return ImageMarkdownRegex.Replace(body, match =>
        {
            var alt = match.Groups[1].Value;
            var path = match.Groups[2].Value;

            // NOTE: deliberately NOT using Uri.TryCreate(path, UriKind.Absolute, ...) here — .NET's
            // Uri parser treats ANY string starting with '/' as a well-formed absolute "file://" URI
            // (e.g. "/assets/x.png" parses successfully as "file:///assets/x.png"), which would wrongly
            // short-circuit our own site-root-relative "/assets/..." scheme before it gets resolved below.
            if (path.Contains("://", StringComparison.Ordinal) || path.StartsWith("data:", StringComparison.Ordinal))
                return match.Value; // already absolute (http(s)://, file://, data:) — leave as-is

            string? resolvedFsPath = null;
            if (path.StartsWith(AssetsUrlPrefix, StringComparison.Ordinal) && _projectPath is not null)
            {
                var relative = path[AssetsUrlPrefix.Length..];
                resolvedFsPath = Path.Combine(_projectPath, "static", relative);
            }
            else if (!path.StartsWith('/'))
            {
                var itemDir = Path.GetDirectoryName(FilePath)!;
                var relative = path.StartsWith("./", StringComparison.Ordinal) ? path[2..] : path;
                resolvedFsPath = Path.Combine(itemDir, relative);
            }

            if (resolvedFsPath is null || !File.Exists(resolvedFsPath))
                return match.Value; // can't resolve — leave the original markdown untouched

            return $"![{alt}]({new Uri(resolvedFsPath).AbsoluteUri})";
        });
    }

    private void RebuildAssetBrowsers()
    {
        if (DocumentAssets is not null)
            DocumentAssets.AssetChosen = null;
        if (FlyoutAssets is not null)
            FlyoutAssets.AssetChosen = null;

        var documentRoot = ResolveAssetDirectory(FilePath);
        DocumentAssets = documentRoot is not null
            ? new AssetBrowserViewModel(_assetLibraryService, _filePicker, _projectPath, documentRoot, isDocumentScoped: true)
            : null;
        if (DocumentAssets is not null)
            DocumentAssets.AssetChosen = OnAssetChosenAsync;

        FlyoutAssets = _projectPath is not null
            ? new AssetBrowserViewModel(_assetLibraryService, _filePicker, _projectPath, Path.Combine(_projectPath, "static"), isDocumentScoped: false)
            : null;
        if (FlyoutAssets is not null)
            FlyoutAssets.AssetChosen = OnAssetChosenAsync;
    }

    private static string? ResolveAssetDirectory(string? filePath)
    {
        if (filePath is null)
            return null;

        var directory = Path.GetDirectoryName(filePath);
        if (directory is null)
            return null;

        if (Path.GetFileName(filePath).Equals("index.md", StringComparison.OrdinalIgnoreCase))
            return directory;

        var slug = Path.GetFileNameWithoutExtension(filePath);
        return Path.Combine(directory, slug);
    }

    private async Task OnAssetChosenAsync(AssetPickerResult result)
    {
        var snippet = await PrepareAssetSnippetAsync(result).ConfigureAwait(true);
        if (snippet is not null)
            AssetSnippetReady?.Invoke(snippet);
    }

    private sealed class NullImageDimensionReader : IImageDimensionReader
    {
        public (int Width, int Height)? TryReadDimensions(string filePath) => null;
    }

    private sealed class NullFilePicker : IFilePicker
    {
        public Task<string?> PickFileAsync(string title) => Task.FromResult<string?>(null);
    }
}
