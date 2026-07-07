namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeSiteSettingsService : ISiteSettingsService
{
    public SiteSettings CurrentSettings { get; set; } =
        new("Test Site", "A test site", "http://localhost:5555/", "en", "default");

    public IReadOnlyList<string> Themes { get; set; } = ["default"];

    public string RawYamlContent { get; set; } = "title: Test Site\n";

    public string? LastSavedProjectPath { get; private set; }
    public SiteSettings? LastSavedSettings { get; private set; }

    public SiteSettings Load(string projectPath) => CurrentSettings;

    public IReadOnlyList<string> ListThemes(string projectPath) => Themes;

    public string ReadRawYaml(string projectPath) => RawYamlContent;

    public void WriteRawYaml(string projectPath, string content) => RawYamlContent = content;

    public void Save(string projectPath, SiteSettings settings)
    {
        LastSavedProjectPath = projectPath;
        LastSavedSettings = settings;
        CurrentSettings = settings;
    }
}
