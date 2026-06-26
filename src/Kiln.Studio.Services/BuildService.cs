namespace Kiln.Studio.Services;

using Kiln.Abstractions;
using Kiln.Services;
using Kiln.Studio.Services.Dto;
using Microsoft.Extensions.DependencyInjection;

public sealed class BuildService : IBuildService
{
    private readonly EngineHost _engineHost;

    public BuildService(EngineHost engineHost)
    {
        _engineHost = engineHost;
    }

    public async Task<BuildSummary> BuildAsync(string projectPath, bool release, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);

        using var provider = _engineHost.CreateProvider(projectPath);
        var siteBuilder = provider.GetRequiredService<ISiteBuilder>();

        var result = release
            ? await siteBuilder.BuildAsync(projectPath, includeDrafts: false, BuildEnvironment.Production, cancellationToken).ConfigureAwait(false)
            : await siteBuilder.BuildAsync(projectPath, includeDrafts: true, BuildEnvironment.Development, cancellationToken).ConfigureAwait(false);

        return new BuildSummary(
            result.Success,
            result.RenderedFiles,
            result.TotalFiles,
            result.SkippedDrafts,
            result.Duration.TotalMilliseconds,
            result.OutputDirectory,
            result.Warnings.ToArray(),
            result.Errors.ToArray());
    }
}