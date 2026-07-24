namespace Kiln.Studio.Services;

/// <summary>
/// Provides read/write access to the <c>menus:</c> block of <c>site.yaml</c> without
/// depending on the Kiln engine. Implementations must preserve the rest of the file.
/// </summary>
public interface IMenuService
{
    IReadOnlyList<MenuDefinition> LoadMenus(string projectPath);
    void SaveMenus(string projectPath, IReadOnlyList<MenuDefinition> menus);
}
