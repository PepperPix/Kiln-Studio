namespace Kiln.Studio.Services;

using System.IO.Compression;
using Kiln.Abstractions;
using Kiln.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed class PublishService : IPublishService
{
    private static readonly HashSet<string> UnsafeRoots =
    [
        "/", "/tmp", "/var", "/etc", "/home", "/Users", "/System",
    ];

    private readonly EngineHost _engineHost;

    public PublishService(EngineHost engineHost)
    {
        _engineHost = engineHost;
    }

    public Task<PublishSummary> PublishAsync(
        string projectPath,
        DeploymentConfig config,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
        ArgumentNullException.ThrowIfNull(config);

        if (config.Variant != DeploymentVariant.Filesystem)
            return Task.FromResult(MakeFailure("Publish is only supported for Filesystem variant."));

        var fsPath = config.FilesystemPath;
        if (string.IsNullOrWhiteSpace(fsPath))
            return Task.FromResult(MakeFailure("Filesystem path is not configured."));

        if (IsUnsafePath(fsPath))
            return Task.FromResult(MakeFailure($"Refusing to publish to unsafe path: {fsPath}"));

        return PublishInternalAsync(projectPath, config, fsPath, cancellationToken);
    }

    private async Task<PublishSummary> PublishInternalAsync(
        string projectPath,
        DeploymentConfig config,
        string filesystemPath,
        CancellationToken cancellationToken)
    {
        try
        {
            using var provider = _engineHost.CreateProvider(projectPath);
            var siteBuilder = provider.GetRequiredService<ISiteBuilder>();

            var buildResult = await siteBuilder.BuildAsync(
                projectPath,
                includeDrafts: false,
                BuildEnvironment.Production,
                cancellationToken).ConfigureAwait(false);

            if (!buildResult.Success)
            {
                var error = buildResult.Errors.Count > 0 ? buildResult.Errors[0] : "Build failed";
                return MakeFailure(error);
            }

            var outputDir = buildResult.OutputDirectory;
            if (!Directory.Exists(outputDir))
                return MakeFailure("Build produced no output directory.");

            if (config.FilesystemMode == FilesystemMode.PlainCopy)
                return await PublishPlainCopyAsync(outputDir, filesystemPath, cancellationToken).ConfigureAwait(false);
            else
                return await PublishZipAsync(outputDir, filesystemPath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return MakeFailure("Publish was cancelled.");
        }
#pragma warning disable CA1031
        catch (Exception ex)
        {
            return MakeFailure(ex.Message);
        }
#pragma warning restore CA1031
    }

    private static async Task<PublishSummary> PublishPlainCopyAsync(
        string sourceDir,
        string targetDir,
        CancellationToken cancellationToken)
    {
        if (Directory.Exists(targetDir))
        {
            foreach (var file in Directory.GetFiles(targetDir))
                File.Delete(file);
            foreach (var dir in Directory.GetDirectories(targetDir))
                Directory.Delete(dir, recursive: true);
        }
        else
        {
            Directory.CreateDirectory(targetDir);
        }

        var fileCount = 0;

        foreach (var sourcePath in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(sourceDir, sourcePath);
            var destPath = Path.Combine(targetDir, relative);
            var destDir = Path.GetDirectoryName(destPath);

            if (destDir is not null)
                Directory.CreateDirectory(destDir);

            File.Copy(sourcePath, destPath, overwrite: true);
            fileCount++;
        }

        await Task.CompletedTask.ConfigureAwait(false);

        return new PublishSummary(true, targetDir, fileCount, null);
    }

    private static async Task<PublishSummary> PublishZipAsync(
        string sourceDir,
        string targetPath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dir = Path.GetDirectoryName(targetPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        if (File.Exists(targetPath))
            File.Delete(targetPath);

        await ZipFile.CreateFromDirectoryAsync(sourceDir, targetPath, CompressionLevel.Optimal, includeBaseDirectory: false, cancellationToken)
            .ConfigureAwait(false);

        return new PublishSummary(true, targetPath, 0, null);
    }

    private static bool IsUnsafePath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimEnd('/');

        if (normalized.Length <= 2)
            return true;

        if (UnsafeRoots.Contains(normalized))
            return true;

        return Path.GetPathRoot(normalized) == normalized;
    }

    private static PublishSummary MakeFailure(string error) =>
        new(false, string.Empty, 0, error);
}
