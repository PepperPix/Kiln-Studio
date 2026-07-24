namespace Kiln.Studio.Services;

using System.IO.Compression;

public sealed class ThemeService : IThemeService
{
    private readonly ISiteSettingsService _siteSettingsService;

    public ThemeService(ISiteSettingsService siteSettingsService)
    {
        _siteSettingsService = siteSettingsService;
    }

    public IReadOnlyList<string> ListThemes(string projectPath)
    {
        return _siteSettingsService.ListThemes(projectPath);
    }

    public string GetCurrentTheme(string projectPath)
    {
        return _siteSettingsService.Load(projectPath).Theme;
    }

    public void SetCurrentTheme(string projectPath, string themeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(themeName);

        var current = _siteSettingsService.Load(projectPath);
        var updated = new SiteSettings(
            current.Title,
            current.Description,
            current.BaseUrl,
            current.Language,
            themeName);

        _siteSettingsService.Save(projectPath, updated);
    }

    public void DuplicateTheme(string projectPath, string sourceThemeName, string newThemeName)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceThemeName);
        ArgumentException.ThrowIfNullOrEmpty(newThemeName);

        var sourcePath = GetThemeDirectoryPath(projectPath, sourceThemeName);
        var targetPath = GetThemeDirectoryPath(projectPath, newThemeName);

        if (!Directory.Exists(sourcePath))
            throw new DirectoryNotFoundException($"Source theme not found: {sourceThemeName}");

        if (Directory.Exists(targetPath))
            throw new InvalidOperationException($"Theme '{newThemeName}' already exists.");

        CopyDirectory(sourcePath, targetPath);
    }

    public string InstallThemeFromZip(string projectPath, string zipFilePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(zipFilePath);

        if (!File.Exists(zipFilePath))
            throw new FileNotFoundException($"ZIP file not found: {zipFilePath}");

        var baseName = Path.GetFileNameWithoutExtension(zipFilePath);
        var themesDir = GetThemesDirectoryPath(projectPath);
        var targetName = GetUniqueDirectoryName(themesDir, baseName);
        var targetPath = Path.Combine(themesDir, targetName);

        Directory.CreateDirectory(targetPath);
        ZipFile.ExtractToDirectory(zipFilePath, targetPath);

        if (!HasLayoutsDirectory(targetPath))
        {
            Directory.Delete(targetPath, recursive: true);
            throw new InvalidDataException("The ZIP file does not contain a valid theme (missing layouts directory).");
        }

        return targetName;
    }

    public IReadOnlyList<ThemeFileEntry> ListThemeFiles(string projectPath, string themeName)
    {
        var themePath = GetThemeDirectoryPath(projectPath, themeName);

        if (!Directory.Exists(themePath))
            return [];

        var entries = new List<ThemeFileEntry>();
        CollectThemeFiles(themePath, themePath, entries);
        entries.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        return entries;
    }

    public string ReadThemeFile(string projectPath, string themeName, string relativePath)
    {
        var filePath = GetThemeFilePath(projectPath, themeName, relativePath);
        return File.ReadAllText(filePath);
    }

    public string GetThemeFilePath(string projectPath, string themeName, string relativePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(relativePath);

        var themePath = GetThemeDirectoryPath(projectPath, themeName);
        var filePath = Path.Combine(themePath, relativePath.TrimStart('/', '\\'));

        var fullPath = Path.GetFullPath(filePath);
        var fullThemePath = Path.GetFullPath(themePath);

        if (!fullPath.StartsWith(fullThemePath, StringComparison.Ordinal))
            throw new InvalidOperationException("File path is outside the theme directory.");

        return fullPath;
    }

    private static string GetThemesDirectoryPath(string projectPath)
    {
        var path = Path.Combine(projectPath, "themes");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    private static string GetThemeDirectoryPath(string projectPath, string themeName)
    {
        return Path.Combine(GetThemesDirectoryPath(projectPath), themeName);
    }

    private static string GetUniqueDirectoryName(string parentDirectory, string baseName)
    {
        var candidate = baseName;
        var counter = 1;

        while (Directory.Exists(Path.Combine(parentDirectory, candidate)))
        {
            candidate = $"{baseName}-{counter}";
            counter++;
        }

        return candidate;
    }

    private static bool HasLayoutsDirectory(string themePath)
    {
        var layoutsPath = Path.Combine(themePath, "layouts");
        if (Directory.Exists(layoutsPath))
            return Directory.EnumerateFiles(layoutsPath, "*", SearchOption.AllDirectories).Any();

        return Directory.EnumerateFileSystemEntries(themePath, "*", SearchOption.AllDirectories)
            .Any(e => e.Contains(Path.DirectorySeparatorChar + "layouts" + Path.DirectorySeparatorChar, StringComparison.Ordinal));
    }

    private static void CollectThemeFiles(string rootPath, string currentPath, List<ThemeFileEntry> entries)
    {
        foreach (var directory in Directory.EnumerateDirectories(currentPath).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var relativePath = GetRelativePath(rootPath, directory);
            entries.Add(new ThemeFileEntry(relativePath, true, ClassifyDirectory(relativePath)));
            CollectThemeFiles(rootPath, directory, entries);
        }

        foreach (var file in Directory.EnumerateFiles(currentPath).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var relativePath = GetRelativePath(rootPath, file);
            entries.Add(new ThemeFileEntry(relativePath, false, ClassifyFile(relativePath)));
        }
    }

    private static string GetRelativePath(string rootPath, string fullPath)
    {
        return Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
    }

    private static ThemeFileKind ClassifyDirectory(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimEnd('/') + "/";
        if (normalized.StartsWith("layouts/", StringComparison.OrdinalIgnoreCase))
            return ThemeFileKind.Layout;

        if (normalized.StartsWith("partials/", StringComparison.OrdinalIgnoreCase))
            return ThemeFileKind.Partial;

        if (normalized.StartsWith("static/", StringComparison.OrdinalIgnoreCase))
            return ThemeFileKind.Static;

        return ThemeFileKind.Other;
    }

    private static ThemeFileKind ClassifyFile(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        if (normalized.StartsWith("layouts/", StringComparison.OrdinalIgnoreCase))
            return ThemeFileKind.Layout;

        if (normalized.StartsWith("partials/", StringComparison.OrdinalIgnoreCase))
            return ThemeFileKind.Partial;

        if (normalized.StartsWith("static/", StringComparison.OrdinalIgnoreCase))
            return ThemeFileKind.Static;

        return ThemeFileKind.Other;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetFilePath = Path.Combine(targetDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath)!);
            File.Copy(file, targetFilePath, overwrite: false);
        }
    }
}
