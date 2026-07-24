namespace Kiln.Studio.Converters;

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

public sealed class DepthToThicknessConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var depth = value as int? ?? 0;
        var indent = depth * 16;
        return new Thickness(indent, 2, 0, 2);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
