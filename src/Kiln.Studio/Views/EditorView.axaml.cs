namespace Kiln.Studio.Views;

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using Kiln.Studio.ViewModels;
using TextMateSharp.Grammars;

public partial class EditorView : UserControl
{
    private EditorViewModel? _currentVm;
    private bool _isSyncing;

    public EditorView()
    {
        InitializeComponent();
        BodyEditor.TextChanged += OnBodyEditorTextChanged;
        DataContextChanged += OnDataContextChanged;
        InstallMarkdownSyntaxHighlighting();

        FrontmatterPanelToggle.IsCheckedChanged += OnFrontmatterPanelToggleChanged;
    }

    private void OnFrontmatterPanelToggleChanged(object? sender, RoutedEventArgs e)
    {
        var isExpanded = FrontmatterPanelToggle.IsChecked == true;
        FrontmatterPanelToggle.Content = isExpanded ? "▾ Frontmatter" : "▸ Frontmatter";

        // The GridSplitter mutates this row's height to an explicit pixel value while dragging.
        // Collapsing/expanding via the toggle button (rather than dragging) resets it back to its
        // natural content size instead of leaving it pinned at whatever height the splitter left.
        DocumentGrid.RowDefinitions[1].Height = isExpanded ? GridLength.Auto : new GridLength(0);
    }

    private void InstallMarkdownSyntaxHighlighting()
    {
        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var installation = TextMate.InstallTextMate(BodyEditor, registryOptions);
        installation.SetGrammar(registryOptions.GetScopeByExtension(".md"));
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentVm is not null)
            _currentVm.PropertyChanged -= OnViewModelPropertyChanged;

        _currentVm = DataContext as EditorViewModel;

        if (_currentVm is not null)
        {
            _currentVm.PropertyChanged += OnViewModelPropertyChanged;
            SyncEditorFromVm(_currentVm);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditorViewModel.Body) && !_isSyncing && _currentVm is not null)
        {
            _isSyncing = true;
            BodyEditor.Text = _currentVm.Body;
            _isSyncing = false;
        }
    }

    private void OnBodyEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isSyncing || _currentVm is null)
            return;

        _isSyncing = true;
        _currentVm.Body = BodyEditor.Text;
        _isSyncing = false;
    }

    private void SyncEditorFromVm(EditorViewModel vm)
    {
        _isSyncing = true;
        BodyEditor.Text = vm.Body;
        _isSyncing = false;
    }
}
