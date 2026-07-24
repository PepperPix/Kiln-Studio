namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Services;

public sealed partial class ThemeManagerViewModel : ViewModelBase
{
    private readonly IThemeService _themeService;
    private readonly IFilePicker _filePicker;
    private readonly IInputDialog _inputDialog;
    private string? _projectPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string? _statusMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyThemeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DuplicateThemeCommand))]
    private string? _selectedTheme;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ApplyThemeCommand))]
    private ThemeFileEntryViewModel? _selectedFile;

    [ObservableProperty]
    private string _selectedFileContent = string.Empty;

    [ObservableProperty]
    private string? _selectedFilePath;

    public ObservableCollection<string> AvailableThemes { get; } = [];

    public ObservableCollection<ThemeFileEntryViewModel> ThemeFiles { get; } = [];

    public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

    public bool IsImageSelected => SelectedFile?.IsImage == true;

    public ThemeManagerViewModel(IThemeService themeService, IFilePicker filePicker, IInputDialog inputDialog)
    {
        _themeService = themeService;
        _filePicker = filePicker;
        _inputDialog = inputDialog;
    }

    public void LoadProject(string projectPath)
    {
        _projectPath = projectPath;
        StatusMessage = null;

        AvailableThemes.Clear();
        foreach (var theme in _themeService.ListThemes(projectPath))
            AvailableThemes.Add(theme);

        SelectedTheme = _themeService.GetCurrentTheme(projectPath);
    }

    public void ClearProject()
    {
        _projectPath = null;
        AvailableThemes.Clear();
        SelectedTheme = null;
        ThemeFiles.Clear();
        SelectedFile = null;
        SelectedFileContent = string.Empty;
        SelectedFilePath = null;
        StatusMessage = null;
    }

    partial void OnSelectedThemeChanged(string? value)
    {
        LoadThemeFiles(value);
    }

    partial void OnSelectedFileChanged(ThemeFileEntryViewModel? value)
    {
        UpdateSelectedFilePath(value);
        LoadSelectedFileContent(value);
        OnPropertyChanged(nameof(IsImageSelected));
    }

    [RelayCommand]
    private void Refresh()
    {
        if (_projectPath is null)
            return;

        var currentTheme = SelectedTheme;
        LoadProject(_projectPath);
        if (AvailableThemes.Contains(currentTheme ?? string.Empty))
            SelectedTheme = currentTheme;
    }

    [RelayCommand(CanExecute = nameof(CanApplyTheme))]
    private void ApplyTheme()
    {
        if (_projectPath is null || string.IsNullOrWhiteSpace(SelectedTheme))
            return;

        try
        {
            _themeService.SetCurrentTheme(_projectPath, SelectedTheme);
            StatusMessage = $"Theme set to '{SelectedTheme}'.";
        }
        catch (IOException ex)
        {
            StatusMessage = $"Failed to apply theme: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Failed to apply theme: {ex.Message}";
        }
    }

    private bool CanApplyTheme() => _projectPath is not null && !string.IsNullOrWhiteSpace(SelectedTheme);

    [RelayCommand(CanExecute = nameof(CanDuplicateTheme))]
    private async Task DuplicateThemeAsync()
    {
        if (_projectPath is null || string.IsNullOrWhiteSpace(SelectedTheme))
            return;

        var newName = await _inputDialog.PromptAsync("Duplicate theme", "New theme name:").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(newName))
            return;

        newName = newName.Trim();
        if (!IsValidThemeName(newName))
        {
            StatusMessage = "Theme name contains invalid characters.";
            return;
        }

        if (AvailableThemes.Contains(newName))
        {
            StatusMessage = $"Theme '{newName}' already exists.";
            return;
        }

        try
        {
            await Task.Run(() => _themeService.DuplicateTheme(_projectPath, SelectedTheme, newName)).ConfigureAwait(true);
            LoadProject(_projectPath);
            SelectedTheme = newName;
            StatusMessage = $"Duplicated theme as '{newName}'.";
        }
        catch (IOException ex)
        {
            StatusMessage = $"Failed to duplicate theme: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Failed to duplicate theme: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = $"Failed to duplicate theme: {ex.Message}";
        }
    }

    private bool CanDuplicateTheme() => _projectPath is not null && !string.IsNullOrWhiteSpace(SelectedTheme);

    [RelayCommand]
    private async Task InstallFromZipAsync()
    {
        if (_projectPath is null)
            return;

        var zipPath = await _filePicker.PickFileAsync("Select theme ZIP file").ConfigureAwait(true);
        if (string.IsNullOrWhiteSpace(zipPath))
            return;

        try
        {
            var installedName = await Task.Run(() => _themeService.InstallThemeFromZip(_projectPath, zipPath)).ConfigureAwait(true);
            LoadProject(_projectPath);
            SelectedTheme = installedName;
            StatusMessage = $"Installed theme '{installedName}'.";
        }
        catch (IOException ex)
        {
            StatusMessage = $"Failed to install theme: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Failed to install theme: {ex.Message}";
        }
        catch (InvalidDataException ex)
        {
            StatusMessage = $"Failed to install theme: {ex.Message}";
        }
    }

    private void LoadThemeFiles(string? themeName)
    {
        ThemeFiles.Clear();
        SelectedFile = null;
        SelectedFileContent = string.Empty;
        SelectedFilePath = null;

        if (_projectPath is null || string.IsNullOrWhiteSpace(themeName))
            return;

        try
        {
            foreach (var entry in _themeService.ListThemeFiles(_projectPath, themeName))
                ThemeFiles.Add(new ThemeFileEntryViewModel(entry));
        }
        catch (IOException ex)
        {
            StatusMessage = $"Failed to list theme files: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Failed to list theme files: {ex.Message}";
        }
    }

    private void UpdateSelectedFilePath(ThemeFileEntryViewModel? file)
    {
        if (_projectPath is null || file is null || file.IsDirectory || string.IsNullOrWhiteSpace(SelectedTheme))
        {
            SelectedFilePath = null;
            return;
        }

        try
        {
            SelectedFilePath = _themeService.GetThemeFilePath(_projectPath, SelectedTheme, file.RelativePath);
        }
        catch (IOException)
        {
            SelectedFilePath = null;
        }
        catch (UnauthorizedAccessException)
        {
            SelectedFilePath = null;
        }
    }

    private void LoadSelectedFileContent(ThemeFileEntryViewModel? file)
    {
        SelectedFileContent = string.Empty;

        if (_projectPath is null || file is null || file.IsDirectory || file.IsImage)
            return;

        try
        {
            SelectedFileContent = _themeService.ReadThemeFile(_projectPath, SelectedTheme ?? string.Empty, file.RelativePath);
        }
        catch (IOException ex)
        {
            StatusMessage = $"Failed to read file: {ex.Message}";
        }
        catch (UnauthorizedAccessException ex)
        {
            StatusMessage = $"Failed to read file: {ex.Message}";
        }
    }

    private static bool IsValidThemeName(string name)
    {
        return name.Length > 0
            && name.All(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            && !name.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !name.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }
}
