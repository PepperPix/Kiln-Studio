namespace Kiln.Studio.TestSupport;

using Services;

public sealed class NullMenuService : IMenuService
{
    public IReadOnlyList<MenuDefinition> LoadMenus(string projectPath) => [];

    public void SaveMenus(string projectPath, IReadOnlyList<MenuDefinition> menus)
    {
    }
}
