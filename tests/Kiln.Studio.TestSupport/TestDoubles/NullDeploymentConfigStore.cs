namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class NullDeploymentConfigStore : IDeploymentConfigStore
{
    public static readonly DeploymentConfig Default = new(DeploymentVariant.None, null, FilesystemMode.PlainCopy);

    public DeploymentConfig Load(string projectPath) => Default;

    public void Save(string projectPath, DeploymentConfig config)
    {
    }
}
