// Vendored from AvaloniaUI/Avalonia (fork JohnCampionJr/Avalonia, branch "tunit", commit ed86219).
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md for details.
using System;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace Avalonia.Headless.TUnit;

/// <summary>
/// Applied at the assembly or class level to opt every TUnit <c>[Test]</c> in scope into the
/// Avalonia headless test pipeline. This single attribute wires up:
/// <list type="bullet">
///   <item><description>
///     The <see cref="AvaloniaClassConstructor"/>, so the class constructor runs on the dispatcher.
///   </description></item>
///   <item><description>
///     The <see cref="AvaloniaTestExecutor"/> as both <see cref="ITestExecutor"/> and
///     <see cref="IHookExecutor"/>, so the test body and <c>[Before(Test)]</c>/<c>[After(Test)]</c>
///     hooks run on the dispatcher.
///   </description></item>
///   <item><description>
///     A <see cref="NotInParallelConstraint"/> on every covered test, so the single-threaded
///     headless dispatcher is never contended.
///   </description></item>
///   <item><description>
///     Per-test pipeline lifetime via <see cref="ITestStartEventReceiver"/> /
///     <see cref="ITestEndEventReceiver"/> &#8212; the pipeline is created lazily for users with a
///     custom <see cref="IClassConstructor"/> and is always disposed after <c>[After(Test)]</c>.
///   </description></item>
/// </list>
/// </summary>
/// <example>
/// <code>
/// [assembly: AvaloniaTestApplication(typeof(MyTestApp))]
/// [assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]
/// [assembly: AvaloniaTest]
/// </code>
/// </example>
public sealed class AvaloniaTestAttribute : ClassConstructorAttribute,
    ITestRegisteredEventReceiver,
    ITestDiscoveryEventReceiver,
    ITestStartEventReceiver,
    ITestEndEventReceiver
{
    private static readonly AvaloniaTestExecutor s_executor = new();

    /// <summary>
    /// Initializes the attribute with <see cref="AvaloniaClassConstructor"/> as the class
    /// constructor that drives test instance creation through the Avalonia dispatcher.
    /// </summary>
    public AvaloniaTestAttribute() : base(typeof(AvaloniaClassConstructor))
    {
    }

    /// <inheritdoc />
    public int Order => 0;

#if NET
    EventReceiverStage ITestStartEventReceiver.Stage => EventReceiverStage.Early;
    EventReceiverStage ITestEndEventReceiver.Stage => EventReceiverStage.Late;
#endif

    /// <inheritdoc />
    public ValueTask OnTestRegistered(TestRegisteredContext context)
    {
        context.SetTestExecutor(s_executor);
        context.SetHookExecutor(s_executor);
        return default;
    }

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(DiscoveredTestContext context)
    {
        // Empty constraint-key array = "do not run in parallel with any other test". Tests share
        // the single-threaded headless dispatcher, so this matches the behaviour of the XUnit
        // PerAssembly project (CollectionPerAssembly + DisableTestParallelization).
        context.AddParallelConstraint(new NotInParallelConstraint(Array.Empty<string>()));
        return default;
    }

    /// <inheritdoc />
    public ValueTask OnTestStart(TestContext context)
    {
        // AvaloniaClassConstructor opens the pipeline at construction time and stashes it in the
        // shared state bag. If a user wires up their own IClassConstructor we open one lazily here
        // so [Before(Test)]/[After(Test)]/test-body still run on the dispatcher.
        if (!context.StateBag.ContainsKey(AvaloniaTestPipeline.StateBagKey))
        {
            var session = HeadlessUnitTestSession.GetOrStartForAssembly(
                context.Metadata.TestDetails.ClassType.Assembly);

            context.StateBag[AvaloniaTestPipeline.StateBagKey] =
                AvaloniaTestPipeline.Create(session, context.Execution.CancellationToken);
        }

        return default;
    }

    /// <inheritdoc />
    public async ValueTask OnTestEnd(TestContext context)
    {
        if (context.StateBag.TryRemove(AvaloniaTestPipeline.StateBagKey, out var stored)
            && stored is AvaloniaTestPipeline pipeline)
        {
            await pipeline.DisposeAsync().ConfigureAwait(false);
        }
    }
}
