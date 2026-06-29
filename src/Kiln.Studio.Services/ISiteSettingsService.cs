namespace Kiln.Studio.Services;

public interface ISiteSettingsService
{
    SiteSettings Load(string projectPath);
    IReadOnlyList<string> ListThemes(string projectPath);
    string ReadRawYaml(string projectPath);
    void WriteRawYaml(string projectPath, string content);
    void Save(string projectPath, SiteSettings settings);
}
