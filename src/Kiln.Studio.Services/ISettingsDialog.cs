namespace Kiln.Studio.Services;

public interface ISettingsDialog
{
    Task ShowAsync(string projectPath);
}
