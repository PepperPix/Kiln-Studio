namespace Kiln.Studio.TestSupport;

using Services;

public sealed class NullMenuRefProvider : IMenuRefProvider
{
    public IReadOnlyList<string> GetCollectionRefs(string projectPath) => [];

    public IReadOnlyList<string> GetItemRefs(string projectPath) => [];
}
