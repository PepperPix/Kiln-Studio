namespace Kiln.Studio.TestSupport;

using ViewModels;

public static class MenuEditorTestFactory
{
    public static MenuEditorViewModel CreateDummy() =>
        new(new NullMenuService(), new NullMenuRefProvider(), new NullInputDialog());
}
