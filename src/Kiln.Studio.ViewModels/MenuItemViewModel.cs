namespace Kiln.Studio.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Services;

public sealed partial class MenuItemViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _title;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private MenuLinkType _linkType;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _ref;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _url;

    [ObservableProperty]
    private bool _external;

    // Collapsed by default so that hover-expand has meaningful work to do and large menus stay overview-friendly.
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _error;

    public MenuItemViewModel? Parent { get; internal set; }

    public ObservableCollection<MenuItemViewModel> Children { get; } = [];

    public bool HasError => !string.IsNullOrEmpty(Error);

    public event EventHandler? ValidationRequested;

    public MenuItemViewModel(MenuItemDefinition definition, MenuItemViewModel? parent)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Parent = parent;
        _title = definition.Title;
        _linkType = definition.LinkType;
        _ref = definition.Ref;
        _url = definition.Url;
        _external = definition.External;

        foreach (var child in definition.Children)
            Children.Add(new MenuItemViewModel(child, this));
    }

    public MenuItemDefinition ToDefinition()
    {
        return new MenuItemDefinition(
            Title,
            LinkType,
            string.IsNullOrWhiteSpace(Ref) ? null : Ref,
            string.IsNullOrWhiteSpace(Url) ? null : Url,
            External,
            Children.Select(c => c.ToDefinition()).ToList());
    }

    partial void OnTitleChanged(string value) => ValidationRequested?.Invoke(this, EventArgs.Empty);

    partial void OnLinkTypeChanged(MenuLinkType value) => ValidationRequested?.Invoke(this, EventArgs.Empty);

    partial void OnRefChanged(string? value) => ValidationRequested?.Invoke(this, EventArgs.Empty);

    partial void OnUrlChanged(string? value)
    {
        if (!External && !string.IsNullOrWhiteSpace(value))
        {
            var trimmed = value.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("//", StringComparison.Ordinal))
            {
                External = true;
            }
        }

        ValidationRequested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnExternalChanged(bool value) => ValidationRequested?.Invoke(this, EventArgs.Empty);
}
