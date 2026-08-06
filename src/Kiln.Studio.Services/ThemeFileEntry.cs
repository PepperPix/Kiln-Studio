namespace Kiln.Studio.Services;

public sealed record ThemeFileEntry(string RelativePath, bool IsDirectory, ThemeFileKind Kind);
