namespace Kiln.Studio.TestSupport;

using Services;

public sealed class NullFolderPicker : IFolderPicker
{
    public Task<string?> PickFolderAsync(string title) => Task.FromResult<string?>(null);
}
