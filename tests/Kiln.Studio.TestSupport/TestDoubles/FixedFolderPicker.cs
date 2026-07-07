namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class FixedFolderPicker(string path) : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(path);
}
