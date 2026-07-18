namespace Kiln.Studio.Views;

using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using Kiln.Studio.ViewModels;
using TextMateSharp.Grammars;

public partial class EditorView : UserControl
{
    private const int RightPanelColumnIndex = 2;

    private EditorViewModel? _currentVm;
    private bool _isSyncing;
    private Flyout? _assetFlyout;

    public EditorView()
    {
        InitializeComponent();
        BodyEditor.TextChanged += OnBodyEditorTextChanged;
        DataContextChanged += OnDataContextChanged;
        InstallMarkdownSyntaxHighlighting();

        RightPanelToggle.IsCheckedChanged += OnRightPanelToggleChanged;
    }

    private void OnRightPanelToggleChanged(object? sender, RoutedEventArgs e)
    {
        var isExpanded = RightPanelToggle.IsChecked == true;

        // The GridSplitter mutates this column's width to an explicit pixel value while dragging.
        // Collapsing/expanding via the toggle button resets it back to its natural width instead of
        // leaving it pinned at whatever width the splitter left.
        DocumentGrid.ColumnDefinitions[RightPanelColumnIndex].Width = isExpanded ? new GridLength(320) : new GridLength(0);
    }

    private void InstallMarkdownSyntaxHighlighting()
    {
        var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
        var installation = TextMate.InstallTextMate(BodyEditor, registryOptions);
        installation.SetGrammar(registryOptions.GetScopeByExtension(".md"));
    }

    // ── Markdown formatting toolbar (PLAN-066) ──────────────────────────────

    private void OnBoldClick(object? sender, RoutedEventArgs e) => WrapSelection("**", "**");

    private void OnItalicClick(object? sender, RoutedEventArgs e) => WrapSelection("_", "_");

    private void OnCodeClick(object? sender, RoutedEventArgs e) => WrapSelection("`", "`");

    private void OnCodeBlockClick(object? sender, RoutedEventArgs e) => WrapSelection("```\n", "\n```");

    private void OnLinkClick(object? sender, RoutedEventArgs e)
    {
        const string placeholder = "url";

        var document = BodyEditor.Document;
        var start = BodyEditor.SelectionStart;
        var length = BodyEditor.SelectionLength;
        var selectedText = document.GetText(start, length);

        document.Replace(start, length, $"[{selectedText}]({placeholder})");

        // Land the selection on the "url" placeholder so the user can type over it immediately,
        // regardless of whether any link text was selected beforehand.
        var placeholderOffset = start + 1 + length + 2;
        BodyEditor.SelectionStart = placeholderOffset;
        BodyEditor.SelectionLength = placeholder.Length;

        BodyEditor.Focus();
    }

    private void OnHeadingClick(object? sender, RoutedEventArgs e)
    {
        // Fixed heading level, current line only — no H1-H6 cycling/toggle (scope cut, PLAN-066).
        var document = BodyEditor.Document;
        var line = document.GetLineByOffset(BodyEditor.SelectionStart);
        document.Insert(line.Offset, "## ");

        BodyEditor.Focus();
    }

    private void OnBulletListClick(object? sender, RoutedEventArgs e) => PrefixLines(_ => "- ");

    private void OnNumberedListClick(object? sender, RoutedEventArgs e) => PrefixLines(index => $"{index + 1}. ");

    private void OnBlockquoteClick(object? sender, RoutedEventArgs e) => PrefixLines(_ => "> ");

    private void OnAssetClick(object? sender, RoutedEventArgs e)
    {
        if (_currentVm?.FlyoutAssets is null)
            return;

        var assetBrowser = new AssetBrowserView
        {
            DataContext = _currentVm.FlyoutAssets
        };

        _assetFlyout = new Flyout
        {
            Content = assetBrowser,
            Placement = PlacementMode.BottomEdgeAlignedLeft
        };

        _assetFlyout.ShowAt(AssetButton);
    }

    private void PrefixLines(Func<int, string> prefixForLineIndex)
    {
        var document = BodyEditor.Document;
        var start = BodyEditor.SelectionStart;
        var length = BodyEditor.SelectionLength;
        var startLineNumber = document.GetLineByOffset(start).LineNumber;
        var endLineNumber = document.GetLineByOffset(start + length).LineNumber;

        // Insert from the last line backwards so earlier lines' offsets stay valid as we mutate.
        for (var lineNumber = endLineNumber; lineNumber >= startLineNumber; lineNumber--)
            document.Insert(document.GetLineByNumber(lineNumber).Offset, prefixForLineIndex(lineNumber - startLineNumber));

        BodyEditor.Focus();
    }

    private void WrapSelection(string prefix, string suffix)
    {
        var document = BodyEditor.Document;
        var start = BodyEditor.SelectionStart;
        var length = BodyEditor.SelectionLength;
        var selectedText = document.GetText(start, length);

        document.Replace(start, length, prefix + selectedText + suffix);

        if (length == 0)
        {
            BodyEditor.CaretOffset = start + prefix.Length;
        }
        else
        {
            BodyEditor.SelectionStart = start + prefix.Length;
            BodyEditor.SelectionLength = length;
        }

        BodyEditor.Focus();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_currentVm is not null)
        {
            _currentVm.PropertyChanged -= OnViewModelPropertyChanged;
            _currentVm.AssetSnippetReady -= OnAssetSnippetReady;
        }

        _currentVm = DataContext as EditorViewModel;

        if (_currentVm is not null)
        {
            _currentVm.PropertyChanged += OnViewModelPropertyChanged;
            _currentVm.AssetSnippetReady += OnAssetSnippetReady;
            SyncEditorFromVm(_currentVm);
        }
    }

    private void OnAssetSnippetReady(string snippet)
    {
        BodyEditor.Document.Insert(BodyEditor.CaretOffset, snippet);
        BodyEditor.Focus();
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
