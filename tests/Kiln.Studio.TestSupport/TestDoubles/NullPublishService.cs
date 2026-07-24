namespace Kiln.Studio.TestSupport;

using Services;

public sealed class NullPublishService : IPublishService
{
    public Task<PublishSummary> PublishAsync(string projectPath, DeploymentConfig config, CancellationToken cancellationToken = default)
        => Task.FromResult(new PublishSummary(true, "/dev/null", 0, null));
}
