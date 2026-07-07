namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FakeBrowserLauncher : IBrowserLauncher
{
    public Uri? LastOpened { get; private set; }

    public void Open(Uri url) => LastOpened = url;
}
