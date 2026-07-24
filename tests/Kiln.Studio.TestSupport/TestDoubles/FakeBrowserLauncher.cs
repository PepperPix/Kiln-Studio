namespace Kiln.Studio.TestSupport;

using Services;

public sealed class FakeBrowserLauncher : IBrowserLauncher
{
    public Uri? LastOpened { get; private set; }

    public void Open(Uri url) => LastOpened = url;
}
