namespace Kiln.Studio.TestSupport;

using Services;

public sealed class NullBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri url)
    {
    }
}
