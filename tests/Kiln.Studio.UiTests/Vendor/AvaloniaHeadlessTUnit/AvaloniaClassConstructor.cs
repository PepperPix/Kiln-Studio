// Vendored from AvaloniaUI/Avalonia (fork JohnCampionJr/Avalonia, branch "tunit", commit ed86219).
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md for details.
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace Avalonia.Headless.TUnit;

/// <summary>
/// Constructs the test class on the Avalonia <see cref="Avalonia.Threading.Dispatcher"/> thread within
/// the test's <see cref="AvaloniaTestPipeline"/>. This is the entry point that opens the pipeline so the
/// constructor, hooks and test body all run against the same isolated <see cref="Application"/>.
/// </summary>
/// <remarks>
/// Wired up automatically when <see cref="AvaloniaTestAttribute"/> is applied at the assembly or class level.
/// Test classes must expose a public parameterless constructor — for constructor-injection scenarios,
/// implement a custom <see cref="IClassConstructor"/> instead.
/// </remarks>
public sealed class AvaloniaClassConstructor : IClassConstructor
{
    /// <inheritdoc />
    public async Task<object> Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        ClassConstructorMetadata classConstructorMetadata)
    {
        var session = HeadlessUnitTestSession.GetOrStartForAssembly(type.Assembly);
        var pipeline = AvaloniaTestPipeline.Create(session, CancellationToken.None);

        // TestBuilderContext.StateBag is the same dictionary that surfaces as TestContext.StateBag.Items,
        // so OnTestStart / OnTestEnd / executors can recover the pipeline without an extra lookup table.
        classConstructorMetadata.TestBuilderContext.StateBag[AvaloniaTestPipeline.StateBagKey] = pipeline;

        return await pipeline.Run(() => new ValueTask<object>(Activator.CreateInstance(type)!))
            .ConfigureAwait(false);
    }
}
