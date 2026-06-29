namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;
using Kiln.Studio.Services.Dto;

public sealed class NullFolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}

public sealed class FixedFolderPicker(string path) : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(path);
}

public sealed class NullInputDialog : IInputDialog
{
    public Task<string?> PromptAsync(string title, string message) => Task.FromResult<string?>(null);
}

public sealed class FixedInputDialog(string response) : IInputDialog
{
    public Task<string?> PromptAsync(string title, string message) => Task.FromResult<string?>(response);
}

public sealed class NullNewPageDialog : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames) => Task.FromResult<NewPageRequest?>(null);
}

public sealed class FixedNewPageDialog(string collectionName, string title) : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames)
        => Task.FromResult<NewPageRequest?>(new NewPageRequest(collectionName, title));
}

public sealed class NullPreviewServer : IPreviewServer
{
    public bool IsRunning => false;
    public Uri? Url => null;

    public Task<Uri> StartAsync(string projectPath) =>
        Task.FromResult(new UriBuilder(Uri.UriSchemeHttp, "localhost", 5000).Uri);

    public void StopServer()
    {
    }
}

public sealed class FakePreviewServer : IPreviewServer
{
    public static readonly Uri FakeUri = new UriBuilder(Uri.UriSchemeHttp, "localhost", 1234).Uri;

    public bool IsRunning { get; private set; }
    public Uri? Url { get; private set; }
    public bool StopCalled { get; private set; }

    public Task<Uri> StartAsync(string projectPath)
    {
        IsRunning = true;
        Url = FakeUri;
        return Task.FromResult(Url);
    }

    public void StopServer()
    {
        StopCalled = true;
        IsRunning = false;
        Url = null;
    }
}

public sealed class NullBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri url)
    {
    }
}

public sealed class FakeBrowserLauncher : IBrowserLauncher
{
    public Uri? LastOpened { get; private set; }

    public void Open(Uri url) => LastOpened = url;
}

public sealed class NullBuildService : IBuildService
{
    public Task<BuildSummary> BuildAsync(
        string projectPath,
        bool release,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new BuildSummary(true, 0, 0, 0, 0, projectPath, [], []));
}

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

public sealed class NullDeploymentService : IDeploymentService
{
    public DeploymentSetupSummary SetUp(
        string projectPath,
        DeployTarget target,
        CancellationToken cancellationToken = default) =>
        new(target, []);
}

public sealed class FakeDeploymentService : IDeploymentService
{
    public Func<string, DeployTarget, CancellationToken, DeploymentSetupSummary>? OnSetUp { get; set; }

    public DeploymentSetupSummary SetUp(string projectPath, DeployTarget target, CancellationToken cancellationToken = default)
    {
        if (OnSetUp is not null)
        {
            return OnSetUp(projectPath, target, cancellationToken);
        }

        return new DeploymentSetupSummary(target, [".github/workflows/deploy.yml"]);
    }
}

public sealed class NullSettingsDialog : ISettingsDialog
{
    public Task ShowAsync(string projectPath) => Task.CompletedTask;
}

public sealed class FakeSiteSettingsService : ISiteSettingsService
{
    public SiteSettings CurrentSettings { get; set; } =
        new("Test Site", "A test site", "http://localhost:5555/", "en", "default");

    public IReadOnlyList<string> Themes { get; set; } = ["default"];

    public string RawYamlContent { get; set; } = "title: Test Site\n";

    public string? LastSavedProjectPath { get; private set; }
    public SiteSettings? LastSavedSettings { get; private set; }

    public SiteSettings Load(string projectPath) => CurrentSettings;

    public IReadOnlyList<string> ListThemes(string projectPath) => Themes;

    public string ReadRawYaml(string projectPath) => RawYamlContent;

    public void WriteRawYaml(string projectPath, string content) => RawYamlContent = content;

    public void Save(string projectPath, SiteSettings settings)
    {
        LastSavedProjectPath = projectPath;
        LastSavedSettings = settings;
        CurrentSettings = settings;
    }
}
