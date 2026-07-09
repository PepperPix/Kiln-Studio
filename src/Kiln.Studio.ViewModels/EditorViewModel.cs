namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public partial class EditorViewModel : ViewModelBase
{
    private readonly IContentService _contentService;
    private readonly IContentFrontmatterWriter _frontmatterWriter;
    private readonly ITaxonomyValueCache _taxonomyValueCache;
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

    public ObservableCollection<TaxonomyFieldViewModel> TaxonomyFields { get; } = [];

    public EditorViewModel(
        IContentService contentService,
        IContentFrontmatterWriter? frontmatterWriter = null,
        ITaxonomyValueCache? taxonomyValueCache = null)
    {
        _contentService = contentService;
        _frontmatterWriter = frontmatterWriter ?? new ContentFrontmatterWriter();
        _taxonomyValueCache = taxonomyValueCache ?? new TaxonomyValueCache();
    }

    public void Load(string filePath, string? projectPath = null, IReadOnlyList<string>? taxonomyNames = null)
    {
        _suppressDirty = true;
        try
        {
            var doc = _contentService.Load(filePath);
            FilePath = filePath;
            FrontMatter = doc.FrontMatter;
            Body = doc.Body;
            HasDocument = true;
            IsDirty = false;
            _projectPath = projectPath;
            LoadTaxonomyFields(filePath, projectPath, taxonomyNames ?? []);
        }
        finally
        {
            _suppressDirty = false;
        }
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

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var filePath = FilePath!;
        var frontMatter = FrontMatter;
        var body = Body;
        var projectPath = _projectPath;
        var taxonomySnapshot = TaxonomyFields
            .Select(f => (f.Name, Values: (IReadOnlyList<string>)f.Values.ToList()))
            .ToList();

        await Task.Run(() =>
        {
            _contentService.Save(filePath, frontMatter, body);
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
}
