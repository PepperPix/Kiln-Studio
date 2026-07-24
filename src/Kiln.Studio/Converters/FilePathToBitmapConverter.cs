namespace Kiln.Studio.Converters;

using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

public sealed class FilePathToBitmapConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || string.IsNullOrWhiteSpace(path))
            return AvaloniaProperty.UnsetValue;

        try
        {
            return new Bitmap(path);
        }
        catch (IOException)
        {
            return AvaloniaProperty.UnsetValue;
        }
        catch (UnauthorizedAccessException)
        {
            return AvaloniaProperty.UnsetValue;
        }
        catch (NotSupportedException)
        {
            return AvaloniaProperty.UnsetValue;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
