namespace Kiln.Studio.Services;

using System.Text;
using System.Text.RegularExpressions;
using Kiln.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed class SiteSettingsService : ISiteSettingsService
{
    private readonly EngineHost _engineHost;

    public SiteSettingsService(EngineHost engineHost)
    {
        _engineHost = engineHost;
    }

    public SiteSettings Load(string projectPath)
    {
        using var provider = _engineHost.CreateProvider(projectPath);
        var loader = provider.GetRequiredService<ISiteConfigLoader>();
        var config = loader.Load(projectPath);
        return new SiteSettings(
            config.Title,
            config.Description ?? string.Empty,
            config.BaseUrl.ToString(),
            config.Language,
            config.Theme);
    }

    public IReadOnlyList<string> ListThemes(string projectPath)
    {
        using var provider = _engineHost.CreateProvider(projectPath);
        var loader = provider.GetRequiredService<ISiteConfigLoader>();
        var config = loader.Load(projectPath);

        var themesPath = Path.IsPathRooted(config.ThemesDir)
            ? config.ThemesDir
            : Path.Combine(projectPath, config.ThemesDir);

        if (!Directory.Exists(themesPath))
            return [];

        return Directory.GetDirectories(themesPath)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string ReadRawYaml(string projectPath)
    {
        return File.ReadAllText(SiteYamlPath(projectPath), Encoding.UTF8);
    }

    public void WriteRawYaml(string projectPath, string content)
    {
        File.WriteAllText(SiteYamlPath(projectPath), content, Encoding.UTF8);
    }

    public void Save(string projectPath, SiteSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var path = SiteYamlPath(projectPath);
        var content = File.ReadAllText(path, Encoding.UTF8);

        content = SetRootScalar(content, "title", settings.Title);
        content = SetRootScalar(content, "description", settings.Description);
        content = SetRootScalar(content, "baseUrl", settings.BaseUrl);
        content = SetRootScalar(content, "language", settings.Language);
        content = SetRootScalar(content, "theme", settings.Theme);

        File.WriteAllText(path, content, Encoding.UTF8);
    }

    private static string SetRootScalar(string content, string key, string value)
    {
        var pattern = $@"^{Regex.Escape(key)}:.*$";
        var replacement = $"{key}: {YamlScalar(value)}";

        if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
            return Regex.Replace(content, pattern, replacement, RegexOptions.Multiline);

        // Key not found — prepend at top of file
        return $"{key}: {YamlScalar(value)}\n{content}";
    }

    private static string YamlScalar(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        if (RequiresYamlQuoting(value))
            return "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
                               .Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

        return value;
    }

    private static bool RequiresYamlQuoting(string value)
    {
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
            return true;

        if (value.Contains(": ", StringComparison.Ordinal) || value.Contains(" #", StringComparison.Ordinal))
            return true;

        const string leadingSpecialChars = "#\"'[]{}*&!%@`|>,";
        if (leadingSpecialChars.Contains(value[0], StringComparison.Ordinal))
            return true;

        return value.Contains('\n', StringComparison.Ordinal) || value.Contains('\r', StringComparison.Ordinal);
    }

    private static string SiteYamlPath(string projectPath)
    {
        var yamlPath = Path.Combine(projectPath, "site.yaml");
        if (File.Exists(yamlPath))
            return yamlPath;

        var ymlPath = Path.Combine(projectPath, "site.yml");
        if (File.Exists(ymlPath))
            return ymlPath;

        throw new FileNotFoundException($"No site.yaml found in: {projectPath}");
    }
}
