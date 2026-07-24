namespace Kiln.Studio.TestSupport;

using Services;

public sealed class NullFilePicker : IFilePicker
{
    public Task<string?> PickFileAsync(string title) => Task.FromResult<string?>(null);
}
