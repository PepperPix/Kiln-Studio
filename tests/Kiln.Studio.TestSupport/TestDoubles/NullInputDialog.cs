namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class NullInputDialog : IInputDialog
{
    public Task<string?> PromptAsync(string title, string message) => Task.FromResult<string?>(null);
}
