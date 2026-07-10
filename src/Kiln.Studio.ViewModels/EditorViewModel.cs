namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public partial class EditorViewModel : ViewModelBase
{
    private static readonly string[] ScalarOwnedKeys = ["title", "date", "description"];
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".svg", ".webp"];

    private readonly IContentService _contentService;
    private readonly IContentFrontmatterWriter _frontmatterWriter;
    private readonly ITaxonomyValueCache _taxonomyValueCache;
    private readonly IAssetPickerDialog _assetPickerDialog;
    private readonly IPageBundleService _pageBundleService;
    private Func<string, Task>? _onPageBundleConverted;
    private bool _suppressDirty;
    private string? _projectPath;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _hasDocument;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _isDirty;

    [ObservableProperty]
    private string _frontMatter = "";

    [ObservableProperty]
    private string _body = "";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private DateTimeOffset? _date;

    [ObservableProperty]
    private string _description = "";

    public ObservableCollection<TaxonomyFieldViewModel> TaxonomyFields { get; } = [];

    public EditorViewModel(
        IContentService contentService,
        IContentFrontmatterWriter? frontmatterWriter = null,
        ITaxonomyValueCache? taxonomyValueCache = null,
        IAssetPickerDialog? assetPickerDialog = null,
        IPageBundleService? pageBundleService = null)
    {
        _contentService = contentService;
        _frontmatterWriter = frontmatterWriter ?? new ContentFrontmatterWriter();
        _taxonomyValueCache = taxonomyValueCache ?? new TaxonomyValueCache();
        _assetPickerDialog = assetPickerDialog ?? new NullAssetPickerDialog();
        _pageBundleService = pageBundleService ?? new PageBundleService();
    }

    /// <summary>
    /// Registers the callback invoked after <see cref="PickAndPrepareAssetAsync"/> converts a flat
    /// content file into a page bundle, so the owner (ShellViewModel) can resynchronize the
    /// explorer with the moved file (analogous to <c>ProjectExplorerViewModel.SetDraftToggleHandler</c>).
    /// </summary>
    public void SetPageBundleConvertedHandler(Func<string, Task> handler) => _onPageBundleConverted = handler;

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
    /// Orchestrates the asset picker/upload/page-bundle-conversion flow and returns the finished
    /// Markdown snippet to insert at the caret, or <see langword="null"/> if the user cancelled.
    /// Deliberately does not touch the body editor control or caret position itself — inserting
    /// the returned snippet is the view's responsibility (see EditorView.axaml.cs).
    /// </summary>
    public async Task<string?> PickAndPrepareAssetAsync()
    {
        var pickerResult = await _assetPickerDialog.ShowAsync(_projectPath!, HasDocument).ConfigureAwait(true);
        if (pickerResult is null)
            return null;

        string markdownPath;
        string fileName;

        if (pickerResult.Destination == AssetPickerDestination.Library)
        {
            var normalized = pickerResult.Path.Replace('\\', '/');
            markdownPath = $"/assets/{normalized}";
            fileName = Path.GetFileName(normalized);
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
        }

        var isImage = ImageExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
        return isImage ? $"![]({markdownPath})" : $"[{fileName}]({markdownPath})";
    }

    private sealed class NullAssetPickerDialog : IAssetPickerDialog
    {
        public Task<AssetPickerResult?> ShowAsync(string projectPath, bool canUploadToPageBundle) =>
            Task.FromResult<AssetPickerResult?>(null);
    }
}
