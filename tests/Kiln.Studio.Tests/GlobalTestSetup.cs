namespace Kiln.Studio.Tests;

using TUnit.Core;

/// <summary>
/// CI has occasionally seen a whole test run hang (not just fail) for hours on macOS/Windows
/// runners instead of finishing in the usual few seconds — e.g. a headless UI test blocking
/// Avalonia's single-threaded dispatch loop forever, which then prevents every other test in the
/// same assembly from ever running (see the ~2h stuck Kiln.Studio.UiTests.dll run investigated on
/// 2026-07-10). A single hung test otherwise blocks the entire process indefinitely with no
/// diagnostic beyond "the job never finished". This sets a generous-but-bounded default per-test
/// timeout so a hang fails fast with a clear "which test timed out" message instead of relying
/// solely on the CI job-level timeout (belt-and-suspenders — see .github/workflows/ci.yml
/// timeout-minutes).
///
/// 30s comfortably covers this project's slowest legitimate tests (pure ViewModel/filesystem
/// tests, plus a handful of debounce tests waiting a couple of seconds) with a large margin, while
/// still catching a true hang in well under a minute rather than hours.
/// </summary>
public static class GlobalTestSetup
{
    private static readonly TimeSpan DefaultTestTimeout = TimeSpan.FromSeconds(30);

    [Before(HookType.TestDiscovery)]
    public static Task Configure(BeforeTestDiscoveryContext context)
    {
        context.Settings.Timeouts.DefaultTestTimeout = DefaultTestTimeout;
        return Task.CompletedTask;
    }
}
