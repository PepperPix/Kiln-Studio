namespace Kiln.Studio.Services;

public interface IPublishService
{
    Task<PublishSummary> PublishAsync(string projectPath, DeploymentConfig config, CancellationToken cancellationToken = default);
}
