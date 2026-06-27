// Vendored from AvaloniaUI/Avalonia (fork JohnCampionJr/Avalonia, branch "tunit", commit ed86219).
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md for details.
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using TUnit.Core;

namespace Avalonia.Headless.TUnit;

/// <summary>
/// Pumps a single <see cref="HeadlessUnitTestSession"/> dispatch call for the lifetime of one TUnit
/// test. All work items submitted via <see cref="Run(System.Func{System.Threading.Tasks.ValueTask})"/>
/// execute against the same Avalonia <see cref="Application"/> and <see cref="Dispatcher"/> instance,
/// so the constructor, <c>[Before(Test)]</c>, the test body and <c>[After(Test)]</c> all observe the
/// same <c>Application.Current</c> in both PerTest and PerAssembly isolation modes.
/// </summary>
internal sealed class AvaloniaTestPipeline : IAsyncDisposable
{
    internal const string StateBagKey = "Avalonia.Headless.TUnit.Pipeline";

    // Completion signal: the long-running session.Dispatch call awaits this; OnTestEnd completes it
    // to let the dispatch return so the isolated/shared Application is torn down.
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Signaled once the session.Dispatch action is actually running on the dispatcher thread.
    // Subsequent Run() calls block on this so they don't race against session setup, and the
    // captured Dispatcher instance is the one Post() will route to.
    private readonly ManualResetEventSlim _dispatcherReady = new(false);

    private readonly Task _runner;
    private Dispatcher? _dispatcher;
    private int _disposed;

    private AvaloniaTestPipeline(HeadlessUnitTestSession session, CancellationToken cancellationToken)
    {
        // IMPORTANT: the trailing `return 0` is load-bearing — it forces this lambda to bind to
        // HeadlessUnitTestSession.Dispatch<TResult>(Func<Task<TResult>>, …) instead of the
        // Dispatch(Action, …) overload, which would treat the async lambda as `async void`,
        // return Task.FromResult(0) synchronously, and end the session before any work runs.
        _runner = session.Dispatch(async () =>
        {
            _dispatcher = Dispatcher.UIThread;
            _dispatcherReady.Set();
            using var ctr = cancellationToken.Register(
                static s => ((TaskCompletionSource<bool>)s!).TrySetResult(false), _completion);
            await _completion.Task.ConfigureAwait(true);
            Dispatcher.UIThread.RunJobs();
            return 0;
        }, cancellationToken);
    }

    public static AvaloniaTestPipeline Create(HeadlessUnitTestSession session, CancellationToken cancellationToken)
        => new(session, cancellationToken);

    /// <summary>
    /// Resolves the active pipeline for the current TUnit test, if any. Returns <c>null</c>
    /// outside of a test scope (e.g. for class/assembly hooks).
    /// </summary>
    public static AvaloniaTestPipeline? TryGetCurrent(TestContext? context)
    {
        if (context is null)
        {
            return null;
        }

        return context.StateBag.TryGetValue<AvaloniaTestPipeline>(StateBagKey, out var pipeline)
            ? pipeline
            : null;
    }

    public ValueTask<T> Run<T>(Func<ValueTask<T>> action)
    {
        _dispatcherReady.Wait();
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher!.Post(() => _ = RunWorkerAsync(action, tcs), DispatcherPriority.Normal);
        return new ValueTask<T>(tcs.Task);
    }

    public ValueTask Run(Func<ValueTask> action)
    {
        _dispatcherReady.Wait();
        var tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcher!.Post(() => _ = RunWorkerAsync(action, tcs), DispatcherPriority.Normal);
        return new ValueTask(tcs.Task);
    }

    private static async Task RunWorkerAsync<T>(Func<ValueTask<T>> action, TaskCompletionSource<T> tcs)
    {
        try
        {
            var result = await action().ConfigureAwait(true);
            tcs.TrySetResult(result);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    private static async Task RunWorkerAsync(Func<ValueTask> action, TaskCompletionSource<object?> tcs)
    {
        try
        {
            await action().ConfigureAwait(true);
            tcs.TrySetResult(null);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _completion.TrySetResult(true);

        try
        {
            await _runner.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _dispatcherReady.Dispose();

            if (_dispatcher?.CheckAccess() == true)
            {
                // HeadlessUnitTestSession completes its task on the dispatcher thread. Don't let the
                // continuation that owns the next test run there — queuing another dispatch from the
                // session's queue thread would deadlock waiting for itself.
                await Task.Run(static () => { }).ConfigureAwait(false);
            }
        }
    }
}
