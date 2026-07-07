namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

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
