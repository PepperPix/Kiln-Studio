namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeDeploymentConfigStore : IDeploymentConfigStore
{
    private DeploymentConfig _config = new(DeploymentVariant.None, null, FilesystemMode.PlainCopy);

    public string? LastSaveProjectPath { get; private set; }
    public DeploymentConfig? LastSaveConfig { get; private set; }

    public DeploymentConfig Config
    {
        get => _config;
        set => _config = value;
    }

    public DeploymentConfig Load(string projectPath) => _config;

    public void Save(string projectPath, DeploymentConfig config)
    {
        LastSaveProjectPath = projectPath;
        LastSaveConfig = config;
        _config = config;
    }
}
