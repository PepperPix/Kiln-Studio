namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class NullSettingsDialog : ISettingsDialog
{
    public Task ShowAsync(string projectPath) => Task.CompletedTask;
}
