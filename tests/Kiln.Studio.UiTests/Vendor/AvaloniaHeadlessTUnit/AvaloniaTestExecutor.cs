// Vendored from AvaloniaUI/Avalonia (fork JohnCampionJr/Avalonia, branch "tunit", commit ed86219).
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md for details.
using System;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Avalonia.Headless.TUnit;

/// <summary>
/// Routes the test body and all per-test (<c>[Before(Test)]</c>/<c>[After(Test)]</c>) hooks
/// through the <see cref="AvaloniaTestPipeline"/> associated with the current
/// <see cref="TestContext"/>. Class/assembly/session hooks have no <see cref="TestContext"/> and
/// therefore fall through to direct execution &#8212; same as the existing XUnit and NUnit packages.
/// </summary>
public sealed class AvaloniaTestExecutor : ITestExecutor, IHookExecutor
{
    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
        => Dispatch(context, action);

    /// <inheritdoc />
    public ValueTask ExecuteBeforeTestHook(MethodMetadata hookMethodInfo, TestContext context, Func<ValueTask> action)
        => Dispatch(context, action);

    /// <inheritdoc />
    public ValueTask ExecuteAfterTestHook(MethodMetadata hookMethodInfo, TestContext context, Func<ValueTask> action)
        => Dispatch(context, action);

    /// <inheritdoc />
    public ValueTask ExecuteBeforeTestDiscoveryHook(MethodMetadata hookMethodInfo, BeforeTestDiscoveryContext context, Func<ValueTask> action)
        => action();

    /// <inheritdoc />
    public ValueTask ExecuteBeforeTestSessionHook(MethodMetadata hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
        => action();

    /// <inheritdoc />
    public ValueTask ExecuteBeforeAssemblyHook(MethodMetadata hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
        => action();

    /// <inheritdoc />
    public ValueTask ExecuteBeforeClassHook(MethodMetadata hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
        => action();

    /// <inheritdoc />
    public ValueTask ExecuteAfterTestDiscoveryHook(MethodMetadata hookMethodInfo, TestDiscoveryContext context, Func<ValueTask> action)
        => action();

    /// <inheritdoc />
    public ValueTask ExecuteAfterTestSessionHook(MethodMetadata hookMethodInfo, TestSessionContext context, Func<ValueTask> action)
        => action();

    /// <inheritdoc />
    public ValueTask ExecuteAfterAssemblyHook(MethodMetadata hookMethodInfo, AssemblyHookContext context, Func<ValueTask> action)
        => action();

    /// <inheritdoc />
    public ValueTask ExecuteAfterClassHook(MethodMetadata hookMethodInfo, ClassHookContext context, Func<ValueTask> action)
        => action();

    private static ValueTask Dispatch(TestContext context, Func<ValueTask> action)
    {
        var pipeline = AvaloniaTestPipeline.TryGetCurrent(context);
        if (pipeline is null)
        {
            return action();
        }

        // The action originates on a TUnit worker thread where TestContext.Current is set; the
        // pipeline pumps work on the Avalonia dispatcher thread where it isn't, so we restore
        // it via MakeCurrent() inside the work item.
        return pipeline.Run(async () =>
        {
            using (context.MakeCurrent())
            {
                await action().ConfigureAwait(false);
            }
        });
    }
}
