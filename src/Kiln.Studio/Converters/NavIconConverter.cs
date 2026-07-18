namespace Kiln.Studio.Converters;

using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

/// <summary>
/// Resolves a <c>NavRailItemViewModel.IconName</c> string (deliberately just a plain string in
/// the Avalonia-free ViewModels project, ADR-054/PLAN-072) to the actual Material.Icons.Avalonia
/// <see cref="MaterialIconKind"/> enum value, here in the View layer where that package dependency
/// belongs (same pattern as <c>IFilePicker</c>/<c>IImageDimensionReader</c>).
/// </summary>
public sealed class NavIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string iconName)
            return null;

        return Enum.TryParse<MaterialIconKind>(iconName, out var kind) ? kind : null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
