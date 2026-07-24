namespace Kiln.Studio.Services;

using Dto;

public interface IBuildService
{
    Task<BuildSummary> BuildAsync(string projectPath, bool release, CancellationToken cancellationToken = default);
}