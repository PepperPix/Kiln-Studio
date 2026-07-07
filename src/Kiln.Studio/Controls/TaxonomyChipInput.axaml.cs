namespace Kiln.Studio.Controls;

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

/// <summary>
/// Reusable "chip" input for a single taxonomy: already-committed values are rendered as
/// removable chips, and a text box (with autocomplete against <see cref="SuggestionsSource"/>)
/// lets the user commit new values by pressing Enter or a comma. Generic over taxonomy name —
/// callers bind <see cref="ItemsSource"/>/<see cref="SuggestionsSource"/> per taxonomy.
/// </summary>
public partial class TaxonomyChipInput : UserControl
{
    public static readonly StyledProperty<ObservableCollection<string>?> ItemsSourceProperty =
        AvaloniaProperty.Register<TaxonomyChipInput, ObservableCollection<string>?>(nameof(ItemsSource));

    public static readonly StyledProperty<IEnumerable<string>?> SuggestionsSourceProperty =
        AvaloniaProperty.Register<TaxonomyChipInput, IEnumerable<string>?>(nameof(SuggestionsSource));

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<TaxonomyChipInput, string?>(nameof(Label));

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
        InputBox.KeyDown += OnInputBoxKeyDown;
        InputBox.TextInput += OnInputBoxTextInput;
    }

    private void OnItemsSourceChanged(AvaloniaPropertyChangedEventArgs e) =>
        ChipsList.ItemsSource = e.NewValue as ObservableCollection<string>;

    private void OnSuggestionsSourceChanged(AvaloniaPropertyChangedEventArgs e) =>
        InputBox.ItemsSource = e.NewValue as IEnumerable<string>;

    private void OnLabelChanged(AvaloniaPropertyChangedEventArgs e)
    {
        var label = e.NewValue as string;
        LabelText.Text = label;
        LabelText.IsVisible = !string.IsNullOrEmpty(label);
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

        var text = InputBox.Text?.Trim();
        InputBox.Text = string.Empty;

        if (string.IsNullOrEmpty(text))
            return;

        if (!items.Contains(text, StringComparer.OrdinalIgnoreCase))
            items.Add(text);
    }

    private void OnRemoveChipClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: string value })
            ItemsSource?.Remove(value);
    }
}
