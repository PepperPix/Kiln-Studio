namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakePublishService : IPublishService
{
    public Func<string, DeploymentConfig, CancellationToken, Task<PublishSummary>>? OnPublishAsync { get; set; }

    public Task<PublishSummary> PublishAsync(string projectPath, DeploymentConfig config, CancellationToken cancellationToken = default)
    {
        if (OnPublishAsync is not null)
            return OnPublishAsync(projectPath, config, cancellationToken);

        return Task.FromResult(new PublishSummary(true, "/output", 42, null));
    }
}
