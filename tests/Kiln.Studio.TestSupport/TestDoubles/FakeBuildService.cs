namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;

public sealed class FakeBuildService : IBuildService
{
    public Func<string, bool, CancellationToken, Task<BuildSummary>>? OnBuildAsync { get; set; }

    public Task<BuildSummary> BuildAsync(string projectPath, bool release, CancellationToken cancellationToken = default)
    {
        if (OnBuildAsync is not null)
        {
            return OnBuildAsync(projectPath, release, cancellationToken);
        }

        return Task.FromResult(new BuildSummary(
            true,
            3,
            3,
            release ? 0 : 1,
            12,
            "/tmp/_site",
            [],
            []));
    }
}
