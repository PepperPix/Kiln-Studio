namespace Kiln.Studio.Services;

/// <summary>
/// Resolves the set of valid <c>ref</c> values for menu entries for the currently open project.
/// </summary>
public interface IMenuRefProvider
{
    IReadOnlyList<string> GetCollectionRefs(string projectPath);

    IReadOnlyList<string> GetItemRefs(string projectPath);
}
