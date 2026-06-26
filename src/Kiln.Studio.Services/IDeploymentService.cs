namespace Kiln.Studio.Services;

using Kiln.Studio.Services.Dto;

public interface IDeploymentService
{
    DeploymentSetupSummary SetUp(string projectPath, DeployTarget target, CancellationToken cancellationToken = default);
}