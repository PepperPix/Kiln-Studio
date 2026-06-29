namespace Kiln.Studio.Converters;

using System.Globalization;
using Avalonia.Data.Converters;
using Kiln.Studio.Services;

public sealed class EnumDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null)
            return string.Empty;

        return value switch
        {
            DraftFilter.All => "All",
            DraftFilter.PublishedOnly => "Published",
            DraftFilter.DraftsOnly => "Drafts",
            ContentSortMode.Default => "Default",
            ContentSortMode.DateNewest => "Date (newest)",
            ContentSortMode.DateOldest => "Date (oldest)",
            ContentSortMode.TitleAscending => "Title A-Z",
            ContentSortMode.TitleDescending => "Title Z-A",
            _ => value.ToString()!
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
