namespace Kiln.Studio.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// A single entry in the left navigation rail. <see cref="IconName"/> is a plain string naming a
/// Material Design icon glyph (e.g. "Note", "CogOutline") - it is deliberately NOT a
/// Material.Icons.Avalonia <c>MaterialIconKind</c> value, since that package is Avalonia-specific
/// and <c>Kiln.Studio.ViewModels</c> must stay Avalonia-free (see repo memory "Lektion 15" /
/// PLAN-069, same pattern as <c>IAssetPickerDialog</c>/<c>IImageDimensionReader</c>). The View
/// layer (<c>Kiln.Studio</c> project) resolves this string to the actual <c>MaterialIconKind</c>
/// enum value via a converter.
/// </summary>
public sealed partial class NavRailItemViewModel : ViewModelBase
{
    public NavTarget Target { get; }
    public string Label { get; }
    public string IconName { get; }

    /// <summary>
    /// True for the four destinations that are navigation-only placeholders in this iteration
    /// (Assets/Menus/Theme/Deployment - real functionality follows in later plans, see ADR-054
    /// "Umfang dieser Iteration").
    /// </summary>
    public bool IsPlaceholder { get; }

    [ObservableProperty]
    private bool _isSelected;

    public NavRailItemViewModel(NavTarget target, string label, string iconName, bool isPlaceholder)
    {
        Target = target;
        Label = label;
        IconName = iconName;
        IsPlaceholder = isPlaceholder;
    }
}
