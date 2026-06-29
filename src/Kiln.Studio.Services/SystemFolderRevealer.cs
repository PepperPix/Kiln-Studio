namespace Kiln.Studio.Services;

using System.Diagnostics;

public sealed class SystemFolderRevealer : IFolderRevealer
{
    public void Reveal(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Directory not found: {path}");

        if (OperatingSystem.IsMacOS())
        {
            Start("open", path);
            return;
        }

        if (OperatingSystem.IsWindows())
        {
            Start("explorer", path);
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            Start("xdg-open", path);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static void Start(string fileName, string path)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = true
        };
        startInfo.ArgumentList.Add(path);
        Process.Start(startInfo);
    }
}