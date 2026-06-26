namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;

sealed class FakeBuildService : IBuildService
{
    public Func<string, bool, CancellationToken, Task<BuildSummary>>? OnBuildAsync { get; set; }

    public Task<BuildSummary> BuildAsync(string projectPath, bool release, CancellationToken cancellationToken = default)
    {
        if (OnBuildAsync is not null)
            return OnBuildAsync(projectPath, release, cancellationToken);

        return Task.FromResult(new BuildSummary(
            true,
            3,
            3,
            release ? 0 : 1,
            12,
            "/tmp/_site",
            [],
            []));
    }
}

sealed class FakeDeploymentService : IDeploymentService
{
    public Func<string, DeployTarget, CancellationToken, DeploymentSetupSummary>? OnSetUp { get; set; }

    public DeploymentSetupSummary SetUp(string projectPath, DeployTarget target, CancellationToken cancellationToken = default)
    {
        if (OnSetUp is not null)
            return OnSetUp(projectPath, target, cancellationToken);

        return new DeploymentSetupSummary(target, [".github/workflows/deploy.yml"]);
    }
}