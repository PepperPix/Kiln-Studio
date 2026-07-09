namespace Kiln.Studio.Controls;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

/// <summary>
/// Reusable "chip" input for a single taxonomy: already-committed values are rendered as
/// removable chips, and a text box (with autocomplete against <see cref="SuggestionsSource"/>)
/// lets the user commit new values by pressing Enter or a comma. Generic over taxonomy name —
/// callers bind <see cref="ItemsSource"/>/<see cref="SuggestionsSource"/> per taxonomy.
///
/// Chips and the input box are direct children of the same <see cref="WrapPanel"/>
/// (<c>ChipsAndInputPanel</c>), so the input always flows right after the last chip and wraps
/// together with them, instead of always sitting in its own row below the chips.
/// </summary>
public partial class TaxonomyChipInput : UserControl
{
    private const double InputBoxMinWidth = 160;
    private const double ChipContentSpacing = 4;

    public static readonly StyledProperty<ObservableCollection<string>?> ItemsSourceProperty =
        AvaloniaProperty.Register<TaxonomyChipInput, ObservableCollection<string>?>(nameof(ItemsSource));

    public static readonly StyledProperty<IEnumerable<string>?> SuggestionsSourceProperty =
        AvaloniaProperty.Register<TaxonomyChipInput, IEnumerable<string>?>(nameof(SuggestionsSource));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<TaxonomyChipInput, string?>(nameof(Label));

    private readonly AutoCompleteBox _inputBox;
    private ObservableCollection<string>? _subscribedItems;

    static TaxonomyChipInput()
    {
        ItemsSourceProperty.Changed.AddClassHandler<TaxonomyChipInput>((o, e) => o.OnItemsSourceChanged(e));
        SuggestionsSourceProperty.Changed.AddClassHandler<TaxonomyChipInput>((o, e) => o.OnSuggestionsSourceChanged(e));
        LabelProperty.Changed.AddClassHandler<TaxonomyChipInput>((o, e) => o.OnLabelChanged(e));
    }

    public ObservableCollection<string>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IEnumerable<string>? SuggestionsSource
    {
        get => GetValue(SuggestionsSourceProperty);
        set => SetValue(SuggestionsSourceProperty, value);
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public TaxonomyChipInput()
    {
        InitializeComponent();

        _inputBox = new AutoCompleteBox
        {
            FilterMode = AutoCompleteFilterMode.Contains,
            PlaceholderText = "Add value, press Enter or comma",
            MinWidth = InputBoxMinWidth,
        };
        _inputBox.KeyDown += OnInputBoxKeyDown;
        _inputBox.TextInput += OnInputBoxTextInput;

        RebuildChips();
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (_subscribedItems is not null)
            _subscribedItems.CollectionChanged -= OnItemsCollectionChanged;

        _subscribedItems = e.NewValue as ObservableCollection<string>;

        if (_subscribedItems is not null)
            _subscribedItems.CollectionChanged += OnItemsCollectionChanged;

        RebuildChips();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildChips();

    private void OnSuggestionsSourceChanged(AvaloniaPropertyChangedEventArgs e) =>
        _inputBox.ItemsSource = e.NewValue as IEnumerable<string>;

    private void OnLabelChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var label = e.NewValue as string;
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
    }

    private void RebuildChips()
    {
        ChipsAndInputPanel.Children.Clear();

        foreach (var value in _subscribedItems ?? [])
            ChipsAndInputPanel.Children.Add(CreateChip(value));

        ChipsAndInputPanel.Children.Add(_inputBox);
    }

    private Border CreateChip(string value)
    {
        var removeButton = new Button { Content = "✕" };
        removeButton.Classes.Add("chip-remove");
        removeButton.Click += (_, _) => ItemsSource?.Remove(value);

        return new Border
        {
            Classes = { "chip" },
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = ChipContentSpacing,
                Children =
                {
                    new TextBlock { Text = value, VerticalAlignment = VerticalAlignment.Center },
                    removeButton,
                },
            },
        };
    }

    private void OnInputBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        CommitPendingText();
        e.Handled = true;
    }

    private void OnInputBoxTextInput(object? sender, TextInputEventArgs e)
    {
        if (e.Text != ",")
            return;

        CommitPendingText();
        e.Handled = true;
    }

    private void CommitPendingText()
    {
        var items = ItemsSource;
        if (items is null)
            return;

        var text = _inputBox.Text?.Trim();
        _inputBox.Text = string.Empty;

        if (string.IsNullOrEmpty(text))
            return;

        if (!items.Contains(text, StringComparer.OrdinalIgnoreCase))
            items.Add(text);
    }
}

