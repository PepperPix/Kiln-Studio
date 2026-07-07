namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

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
