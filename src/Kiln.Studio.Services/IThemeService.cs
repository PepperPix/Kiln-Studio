namespace Kiln.Studio.Services;

public interface IThemeService
{
    IReadOnlyList<string> ListThemes(string projectPath);

    string GetCurrentTheme(string projectPath);

    void SetCurrentTheme(string projectPath, string themeName);

    void DuplicateTheme(string projectPath, string sourceThemeName, string newThemeName);

    string InstallThemeFromZip(string projectPath, string zipFilePath);

    IReadOnlyList<ThemeFileEntry> ListThemeFiles(string projectPath, string themeName);

    string ReadThemeFile(string projectPath, string themeName, string relativePath);

    string GetThemeFilePath(string projectPath, string themeName, string relativePath);
}

public sealed record ThemeFileEntry(string RelativePath, bool IsDirectory, ThemeFileKind Kind);

public enum ThemeFileKind
{
    Layout,
    Partial,
    Static,
    Other
}
