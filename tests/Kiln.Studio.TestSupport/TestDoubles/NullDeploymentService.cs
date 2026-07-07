namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;

public sealed class NullDeploymentService : IDeploymentService
{
    public DeploymentSetupSummary SetUp(
        string projectPath,
        DeployTarget target,
        CancellationToken cancellationToken = default) =>
        new(target, []);
}
