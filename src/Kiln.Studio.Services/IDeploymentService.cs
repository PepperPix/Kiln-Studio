namespace Kiln.Studio.Services;

using Dto;

public interface IDeploymentService
{
    DeploymentSetupSummary SetUp(string projectPath, DeployTarget target, CancellationToken cancellationToken = default);
}