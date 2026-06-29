namespace Kiln.Studio.Services;

public interface IDeploymentConfigStore
{
    DeploymentConfig Load(string projectPath);
    void Save(string projectPath, DeploymentConfig config);
}
