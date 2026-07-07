namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;

public sealed class NullBuildService : IBuildService
{
    public Task<BuildSummary> BuildAsync(
        string projectPath,
        bool release,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new BuildSummary(true, 0, 0, 0, 0, projectPath, [], []));
}
