namespace Kiln.Studio.Tests;

using Kiln.Services;
using Services;
using Services.Dto;
using Microsoft.Extensions.DependencyInjection;

public class DeploymentServiceTests
{
    [Test]
    public async Task SetUp_GitHubPages_CreatesWorkflowFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectPath = CreateSite(tempDir, "deploy-gh");
            var service = new DeploymentService(new EngineHost());

            var result = service.SetUp(projectPath, DeployTarget.GitHubPages);

            await Assert.That(result.Target).IsEqualTo(DeployTarget.GitHubPages);
            await Assert.That(result.CreatedFiles).Contains(".github/workflows/deploy.yml");
            await Assert.That(File.Exists(Path.Combine(projectPath, ".github", "workflows", "deploy.yml"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public async Task SetUp_AzureStaticWebApps_CreatesWorkflowFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        try
        {
            var projectPath = CreateSite(tempDir, "deploy-swa");
            var service = new DeploymentService(new EngineHost());

            var result = service.SetUp(projectPath, DeployTarget.AzureStaticWebApps);

            await Assert.That(result.Target).IsEqualTo(DeployTarget.AzureStaticWebApps);
            await Assert.That(result.CreatedFiles).Contains(".github/workflows/azure-swa.yml");
            await Assert.That(File.Exists(Path.Combine(projectPath, ".github", "workflows", "azure-swa.yml"))).IsTrue();
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateSite(string parentDir, string siteName)
    {
        var host = new EngineHost();
        using var provider = host.CreateProvider(parentDir);
        var scaffolder = provider.GetRequiredService<IScaffolder>();
        return scaffolder.CreateSite(siteName, parentDir).ProjectPath;
    }
}
