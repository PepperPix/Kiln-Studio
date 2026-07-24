namespace Kiln.Studio.ViewModels;

using Services;

public sealed class ThemeFileEntryViewModel : ViewModelBase
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.Ordinal)
    {
        ".PNG",
        ".JPG",
        ".JPEG",
        ".GIF",
        ".WEBP",
        ".SVG"
    };

    public ThemeFileEntry Entry { get; }

    public string RelativePath => Entry.RelativePath;

    public bool IsDirectory => Entry.IsDirectory;

    public ThemeFileKind Kind => Entry.Kind;

    public string IconKind => GetIconKind(Entry);

    public bool IsImage => !Entry.IsDirectory && IsImageExtension(Entry.RelativePath);

    public int Depth
    {
        get
        {
            var trimmed = Entry.RelativePath.Trim('/');
            return string.IsNullOrEmpty(trimmed) ? 0 : trimmed.Count(c => c == '/');
        }
    }

    public ThemeFileEntryViewModel(ThemeFileEntry entry)
    {
        Entry = entry;
    }

    private static string GetIconKind(ThemeFileEntry entry)
    {
        if (entry.IsDirectory)
            return "FolderOutline";

        if (IsImageExtension(entry.RelativePath))
            return "ImageOutline";

        return entry.Kind switch
        {
            ThemeFileKind.Layout => "CodeBraces",
            ThemeFileKind.Partial => "CodeTags",
            ThemeFileKind.Static when IsCssExtension(entry.RelativePath) => "FileCss",
            _ => "FileOutline"
        };
    }

    private static bool IsImageExtension(string path)
    {
        var extension = Path.GetExtension(path).ToUpperInvariant();
        return ImageExtensions.Contains(extension);
    }

    private static bool IsCssExtension(string path)
    {
        return string.Equals(Path.GetExtension(path), ".css", StringComparison.OrdinalIgnoreCase);
    }
}
