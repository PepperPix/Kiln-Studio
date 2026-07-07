namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;

public sealed class FakeDeploymentService : IDeploymentService
{
    public Func<string, DeployTarget, CancellationToken, DeploymentSetupSummary>? OnSetUp { get; set; }

    public DeploymentSetupSummary SetUp(string projectPath, DeployTarget target, CancellationToken cancellationToken = default)
    {
        if (OnSetUp is not null)
        {
            return OnSetUp(projectPath, target, cancellationToken);
        }

        return new DeploymentSetupSummary(target, [".github/workflows/deploy.yml"]);
    }
}
