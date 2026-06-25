namespace Kiln.Studio.Services;

public sealed class ProjectSession : IDisposable
{
    public string? ProjectPath { get; set; }
    public OpenedProject? Current { get; set; }

    private IDisposable? _providerLifetime;

    public void SetProvider(IDisposable provider)
    {
        _providerLifetime?.Dispose();
        _providerLifetime = provider;
    }

    public void Dispose()
    {
        _providerLifetime?.Dispose();
        _providerLifetime = null;
    }
}
