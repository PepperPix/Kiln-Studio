namespace Kiln.Studio.Services;

using System.Diagnostics;

public sealed class SystemBrowserLauncher : IBrowserLauncher
{
    public void Open(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);
        Process.Start(new ProcessStartInfo(url.ToString()) { UseShellExecute = true });
    }
}
