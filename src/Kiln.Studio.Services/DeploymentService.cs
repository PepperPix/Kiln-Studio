namespace Kiln.Studio.Services;

using Kiln.Models;
using Kiln.Services;
using Kiln.Studio.Services.Dto;
using Microsoft.Extensions.DependencyInjection;

public sealed class DeploymentService : IDeploymentService
{
    private readonly EngineHost _engineHost;

    public DeploymentService(EngineHost engineHost)
    {
        _engineHost = engineHost;
    }

    public DeploymentSetupSummary SetUp(string projectPath, DeployTarget target, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        using var provider = _engineHost.CreateProvider(projectPath);
        var initializer = provider.GetRequiredService<IDeploymentInitializer>();

        var result = initializer.Initialize(Map(target), projectPath, cancellationToken);
        return new DeploymentSetupSummary(target, result.CreatedFiles.ToArray());
    }

    private static DeploymentTarget Map(DeployTarget target) => target switch
    {
        DeployTarget.GitHubPages => DeploymentTarget.GitHubPages,
        DeployTarget.AzureStaticWebApps => DeploymentTarget.AzureStaticWebApps,
        _ => throw new InvalidOperationException($"Unsupported deployment target: {target}"),
    };
}