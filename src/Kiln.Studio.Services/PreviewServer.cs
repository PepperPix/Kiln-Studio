namespace Kiln.Studio.Services;

using System.Net;
using System.Net.Sockets;
using Kiln.Services;
using Microsoft.Extensions.DependencyInjection;

public sealed class PreviewServer : IPreviewServer, IDisposable
{
    private const int PollIntervalMs = 300;

    private readonly EngineHost _engineHost;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public bool IsRunning { get; private set; }
    public Uri? Url { get; private set; }

    public PreviewServer(EngineHost engineHost)
    {
        _engineHost = engineHost;
    }

    public async Task<Uri> StartAsync(string projectPath)
    {
        if (IsRunning && Url is not null)
            return Url;

        var port = FindFreePort();
        var uri = new Uri($"http://localhost:{port}/");

        var provider = _engineHost.CreateProvider(projectPath);
        var devServer = provider.GetRequiredService<IDevServer>();

        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        _serverTask = Task.Run(() => devServer.RunAsync(projectPath, port, ct: ct), ct);

        var ready = await PollForReadyAsync(uri, TimeSpan.FromSeconds(15), ct).ConfigureAwait(false);
        if (!ready)
        {
            StopServer();
            throw new InvalidOperationException($"Preview server did not respond on {uri} within timeout.");
        }

        Url = uri;
        IsRunning = true;
        return uri;
    }

    public void StopServer()
    {
        _cts?.Cancel();
        _serverTask = null;
        IsRunning = false;
        Url = null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _serverTask = null;
        IsRunning = false;
        Url = null;
    }

    private static int FindFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static async Task<bool> PollForReadyAsync(Uri uri, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromSeconds(2);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            try
            {
                var resp = await http.GetAsync(uri, ct).ConfigureAwait(false);
                if (resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.NotFound)
                    return true;
            }
#pragma warning disable CA1031
            catch
            {
                // server not ready yet — keep polling
            }
#pragma warning restore CA1031

            await Task.Delay(PollIntervalMs, ct).ConfigureAwait(false);
        }

        return false;
    }
}
