namespace Kiln.Studio.UiTests;

using TUnit.Core;

/// <summary>
/// See Kiln.Studio.Tests.GlobalTestSetup for the rationale. 60s (rather than that project's 30s)
/// to leave headroom for headless Avalonia rendering/screenshot I/O and the larger seeded-project
/// exploratory tour test, while still catching a true hang in well under a minute rather than
/// hours.
/// </summary>
public static class GlobalTestSetup
{
    private static readonly TimeSpan DefaultTestTimeout = TimeSpan.FromSeconds(60);

    [Before(HookType.TestDiscovery)]
    public static Task Configure(BeforeTestDiscoveryContext context)
    {
        context.Settings.Timeouts.DefaultTestTimeout = DefaultTestTimeout;
        return Task.CompletedTask;
    }
}
