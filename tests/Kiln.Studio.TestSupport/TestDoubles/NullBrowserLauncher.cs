namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class NullBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri url)
    {
    }
}
