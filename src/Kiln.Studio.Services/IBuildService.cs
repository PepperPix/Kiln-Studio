namespace Kiln.Studio.Services;

using Kiln.Studio.Services.Dto;

public interface IBuildService
{
    Task<BuildSummary> BuildAsync(string projectPath, bool release, CancellationToken cancellationToken = default);
}