namespace Kiln.Studio.TestSupport;

using Kiln.Studio.Services;

public sealed class NullNewPageDialog : INewPageDialog
{
    public Task<NewPageRequest?> ShowAsync(IReadOnlyList<string> collectionNames) => Task.FromResult<NewPageRequest?>(null);
}
