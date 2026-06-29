namespace Kiln.Studio.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public partial class EditorViewModel : ViewModelBase
{
    private readonly IContentService _contentService;
    private bool _suppressDirty;

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

    public EditorViewModel(IContentService contentService)
    {
        _contentService = contentService;
    }

    public void Load(string filePath)
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
        }
        finally
        {
            _suppressDirty = false;
        }
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
        await Task.Run(() => _contentService.Save(filePath, frontMatter, body)).ConfigureAwait(true);
        IsDirty = false;
    }

    private bool CanSave() => HasDocument && IsDirty && FilePath is not null;
}
