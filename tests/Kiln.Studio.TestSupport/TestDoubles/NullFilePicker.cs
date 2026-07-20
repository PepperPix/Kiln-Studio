namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class NullFilePicker : IFilePicker
{
    public Task<string?> PickFileAsync(string title) => Task.FromResult<string?>(null);
}
