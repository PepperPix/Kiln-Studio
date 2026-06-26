namespace Kiln.Studio.Services;

public interface IPreviewServer
{
    bool IsRunning { get; }
    Uri? Url { get; }
    Task<Uri> StartAsync(string projectPath);
    void StopServer();
}
