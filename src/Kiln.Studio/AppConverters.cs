namespace Kiln.Studio;

using Avalonia.Data.Converters;

/// <summary>Shared view-layer value converters (no ViewModel dependency).</summary>
internal static class AppConverters
{
    /// <summary>Returns true when the bound int value equals zero (used for empty-state visibility).</summary>
    public static readonly FuncValueConverter<int?, bool> IsCountZero =
        new(count => count is null or 0);
}
