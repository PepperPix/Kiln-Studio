namespace Kiln.Studio.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class DeploymentConfigStore : IDeploymentConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) },
    };

    public DeploymentConfig Load(string projectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        var path = DeployJsonPath(projectPath);
        if (!File.Exists(path))
            return new DeploymentConfig(DeploymentVariant.None, null, FilesystemMode.PlainCopy);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<DeploymentConfig>(json, JsonOptions)
            ?? new DeploymentConfig(DeploymentVariant.None, null, FilesystemMode.PlainCopy);
    }

    public void Save(string projectPath, DeploymentConfig config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentNullException.ThrowIfNull(config);

        var path = DeployJsonPath(projectPath);
        var dir = Path.GetDirectoryName(path);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static string DeployJsonPath(string projectPath) =>
        Path.Combine(projectPath, ".kiln", "deploy.json");
}
