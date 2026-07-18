namespace Kiln.Studio;

using Avalonia.Data.Converters;
using Kiln.Studio.Services;
using Material.Icons;

/// <summary>Shared view-layer value converters (no ViewModel dependency).</summary>
internal static class AppConverters
{
    /// <summary>Returns true when the bound nullable int value equals zero or null.</summary>
    public static readonly FuncValueConverter<int?, bool> IsCountZero =
        new(count => count is null or 0);

    /// <summary>Returns true when the bound int value equals zero (used for empty-state visibility).</summary>
    public static readonly FuncValueConverter<int, bool> IsCountZeroInt =
        new(count => count == 0);

    /// <summary>Returns true when the bound string is not null or empty.</summary>
    public static readonly FuncValueConverter<string?, bool> IsStringNotNullOrEmpty =
        new(value => !string.IsNullOrEmpty(value));

    /// <summary>
    /// Two-way converter for a radio button that should be checked when the destination is
    /// <see cref="AssetPickerDestination.PageBundle"/>.
    /// </summary>
    public static readonly FuncValueConverter<AssetPickerDestination, bool> DestinationIsPageBundle =
        new(
            dest => dest == AssetPickerDestination.PageBundle,
            isChecked => isChecked ? AssetPickerDestination.PageBundle : AssetPickerDestination.Library);

    /// <summary>
    /// Two-way converter for a radio button that should be checked when the destination is
    /// <see cref="AssetPickerDestination.Library"/>.
    /// </summary>
    public static readonly FuncValueConverter<AssetPickerDestination, bool> DestinationIsLibrary =
        new(
            dest => dest == AssetPickerDestination.Library,
            isChecked => isChecked ? AssetPickerDestination.Library : AssetPickerDestination.PageBundle);

    /// <summary>Nav rail width (ADR-054/PLAN-072): wide when expanded (icon+label), narrow icon-only when collapsed.</summary>
    public static readonly FuncValueConverter<bool, double> ExpandedToNavRailWidth =
        new(isExpanded => isExpanded ? 200 : 56);

    /// <summary>Chevron direction for the nav rail's own expand/collapse toggle button.</summary>
    public static readonly FuncValueConverter<bool, MaterialIconKind> ExpandedToCollapseIcon =
        new(isExpanded => isExpanded ? MaterialIconKind.ChevronLeft : MaterialIconKind.ChevronRight);

    /// <summary>Dims nav rail entries that are navigation-only placeholders in this iteration
    /// (ADR-054 "Umfang dieser Iteration") so they read as visually distinct from real destinations.</summary>
    public static readonly FuncValueConverter<bool, double> PlaceholderToOpacity =
        new(isPlaceholder => isPlaceholder ? 0.6 : 1.0);

    /// <summary>
    /// Chevron for the editor's unified right panel collapse handle (EditorView, ADR-056/PLAN-074).
    /// Points left when the panel is expanded (towards the panel), right when collapsed, mirroring
    /// the nav rail's chevron convention. Replaces the previous pair of left/right panel converters
    /// now that only one gutter toggle remains.
    /// </summary>
    public static readonly FuncValueConverter<bool, MaterialIconKind> ExpandedToChevron =
        new(isExpanded => isExpanded ? MaterialIconKind.ChevronLeft : MaterialIconKind.ChevronRight);
}
