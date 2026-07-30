namespace Kiln.Studio.Views;

using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using ViewModels;

public partial class ThemeManagerView : UserControl
{
    private TextMate.Installation? _textMateInstallation;

    public ThemeManagerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not ThemeManagerViewModel vm)
            return;

        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(ThemeManagerViewModel.SelectedFile)
                or nameof(ThemeManagerViewModel.SelectedFileContent))
            {
                UpdatePreview(vm.SelectedFile, vm.SelectedFileContent);
            }
        };

        UpdatePreview(vm.SelectedFile, vm.SelectedFileContent);
    }

    private void UpdatePreview(ThemeFileEntryViewModel? selectedFile, string content)
    {
        if (PreviewEditor is null)
            return;

        PreviewEditor.Text = content;
        _textMateInstallation?.Dispose();
        _textMateInstallation = null;

        var scope = GetGrammarScope(selectedFile);
        if (scope is null)
            return;

        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        _textMateInstallation = TextMate.InstallTextMate(PreviewEditor, registryOptions);
        _textMateInstallation.SetGrammar(scope);
    }

    private static string? GetGrammarScope(ThemeFileEntryViewModel? selectedFile)
    {
        if (selectedFile is null || selectedFile.IsDirectory)
            return null;

        var extension = Path.GetExtension(selectedFile.RelativePath);
        if (string.IsNullOrEmpty(extension))
            return null;

        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        return registryOptions.GetScopeByExtension(extension);
    }
}
