namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kiln.Studio.Services;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ISiteSettingsService _settings;
    private string? _projectPath;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _baseUrl = string.Empty;

    [ObservableProperty]
    private string _language = string.Empty;

    [ObservableProperty]
    private string? _selectedTheme;

    [ObservableProperty]
    private string _rawYaml = string.Empty;

    [ObservableProperty]
    private bool _isAdvanced;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string? _statusMessage;

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public ObservableCollection<string> AvailableThemes { get; } = [];

    public SettingsViewModel(ISiteSettingsService settings)
    {
        _settings = settings;
    }

    public void Load(string projectPath)
    {
        _projectPath = projectPath;

        var s = _settings.Load(projectPath);
        Title = s.Title;
        Description = s.Description;
        BaseUrl = s.BaseUrl;
        Language = s.Language;

        AvailableThemes.Clear();
        foreach (var theme in _settings.ListThemes(projectPath))
            AvailableThemes.Add(theme);

        SelectedTheme = s.Theme;
        RawYaml = _settings.ReadRawYaml(projectPath);
        StatusMessage = null;
    }

    partial void OnIsAdvancedChanged(bool value)
    {
        if (_projectPath is null)
            return;

        if (value)
        {
            RawYaml = _settings.ReadRawYaml(_projectPath);
            StatusMessage = "Unsaved basic form changes have been discarded.";
        }
        else
        {
            var s = _settings.Load(_projectPath);
            Title = s.Title;
            Description = s.Description;
            BaseUrl = s.BaseUrl;
            Language = s.Language;
            SelectedTheme = s.Theme;
            StatusMessage = "Unsaved raw YAML changes have been discarded.";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_projectPath is null)
            return;

        try
        {
            if (IsAdvanced)
            {
                await Task.Run(() => _settings.WriteRawYaml(_projectPath, RawYaml)).ConfigureAwait(true);
            }
            else
            {
                var s = new SiteSettings(Title, Description, BaseUrl, Language, SelectedTheme ?? string.Empty);
                await Task.Run(() => _settings.Save(_projectPath, s)).ConfigureAwait(true);
            }

            Load(_projectPath);
            StatusMessage = "Settings saved.";
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            StatusMessage = $"Save failed: {ex.Message}";
        }
#pragma warning restore CA1031
    }
}
