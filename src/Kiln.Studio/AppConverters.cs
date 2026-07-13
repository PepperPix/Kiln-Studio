namespace Kiln.Studio;

using Avalonia.Data.Converters;
using Material.Icons;

/// <summary>Shared view-layer value converters (no ViewModel dependency).</summary>
internal static class AppConverters
{
    /// <summary>Returns true when the bound int value equals zero (used for empty-state visibility).</summary>
    public static readonly FuncValueConverter<int?, bool> IsCountZero =
        new(count => count is null or 0);

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

    /// <summary>Chevron for the Frontmatter panel's collapse handle (EditorView) — points left
    /// (towards the panel it collapses) when expanded, right when collapsed, mirroring the nav
    /// rail's own chevron convention.</summary>
    public static readonly FuncValueConverter<bool, MaterialIconKind> ExpandedToLeftPanelCollapseIcon =
        new(isExpanded => isExpanded ? MaterialIconKind.ChevronLeft : MaterialIconKind.ChevronRight);

    /// <summary>Chevron for the inline Preview panel's collapse handle (EditorView) — points right
    /// (towards the panel it reveals) when hidden, left when visible; mirrored orientation of
    /// <see cref="ExpandedToLeftPanelCollapseIcon"/> since the Preview panel lives on the opposite
    /// (right) side of the editor.</summary>
    public static readonly FuncValueConverter<bool, MaterialIconKind> VisibleToRightPanelCollapseIcon =
        new(isVisible => isVisible ? MaterialIconKind.ChevronRight : MaterialIconKind.ChevronLeft);
}
