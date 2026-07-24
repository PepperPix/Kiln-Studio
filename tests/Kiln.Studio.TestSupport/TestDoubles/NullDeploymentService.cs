namespace Kiln.Studio.TestSupport;

using Services;
using Services.Dto;

public sealed class NullDeploymentService : IDeploymentService
{
    public DeploymentSetupSummary SetUp(
        string projectPath,
        DeployTarget target,
        CancellationToken cancellationToken = default) =>
        new(target, []);
}
