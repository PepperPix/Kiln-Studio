namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;
using Kiln.Studio.TestSupport;
using Kiln.Studio.ViewModels;

public class MenuEditorViewModelTests
{
    private const int ExpectedTwoItems = 2;
    [Test]
    public async Task LoadProject_PopulatesMenusAndSuggestions()
    {
        var path = CreateSiteWithMenus();
        try
        {
            var service = new MenuService();
            var loaded = service.LoadMenus(path);
            await Assert.That(loaded.Count).IsEqualTo(1);

            var vm = MakeVm(path);

            await Assert.That(vm.Menus.Count).IsEqualTo(1);
            await Assert.That(vm.Menus[0].Name).IsEqualTo("main");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task LoadProject_WithRefProvider_PopulatesSuggestions()
    {
        var path = CreateSiteWithMenus();
        try
        {
            var vm = new MenuEditorViewModel(
                new MenuService(),
                new MenuRefProvider(new EngineHost()),
                new NullInputDialog());
            vm.LoadProject(path);

            await Assert.That(vm.RefSuggestions).Contains("posts/hello");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task AddItem_AddsItemToSelectedMenu()
    {
        var path = CreateSiteWithMenus();
        try
        {
            var vm = MakeVm(path);

            vm.AddItemCommand.Execute(null);

            await Assert.That(vm.SelectedMenu!.Items.Count).IsEqualTo(ExpectedTwoItems);
            await Assert.That(vm.SelectedItem).IsNotNull();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task DeleteItem_RemovesSelectedItem()
    {
        var path = CreateSiteWithMenus();
        try
        {
            var vm = MakeVm(path);
            vm.SelectedItem = vm.SelectedMenu!.Items[0];

            vm.DeleteItemCommand.Execute(null);

            await Assert.That(vm.SelectedMenu.Items).IsEmpty();
            await Assert.That(vm.SelectedItem).IsNull();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task MoveUp_FirstItem_DoesNotMove()
    {
        var path = CreateSiteWithTwoItems();
        try
        {
            var vm = MakeVm(path);
            vm.SelectedItem = vm.SelectedMenu!.Items[0];

            vm.MoveUpCommand.Execute(null);

            await Assert.That(vm.SelectedMenu.Items[0].Title).IsEqualTo("First");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task MoveDown_FirstItem_SwapsWithSecond()
    {
        var path = CreateSiteWithTwoItems();
        try
        {
            var vm = MakeVm(path);
            vm.SelectedItem = vm.SelectedMenu!.Items[0];

            vm.MoveDownCommand.Execute(null);

            await Assert.That(vm.SelectedMenu.Items[0].Title).IsEqualTo("Second");
            await Assert.That(vm.SelectedMenu.Items[1].Title).IsEqualTo("First");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task Indent_SecondItem_BecomesChildOfFirst()
    {
        var path = CreateSiteWithTwoItems();
        try
        {
            var vm = MakeVm(path);
            vm.SelectedItem = vm.SelectedMenu!.Items[1];

            vm.IndentCommand.Execute(null);

            await Assert.That(vm.SelectedMenu.Items.Count).IsEqualTo(1);
            await Assert.That(vm.SelectedMenu.Items[0].Children.Count).IsEqualTo(1);
            await Assert.That(vm.SelectedMenu.Items[0].Children[0].Title).IsEqualTo("Second");
            await Assert.That(vm.SelectedItem!.Parent).IsEqualTo(vm.SelectedMenu.Items[0]);
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task Outdent_ChildItem_BecomesSiblingAfterParent()
    {
        var path = CreateSiteWithNestedItems();
        try
        {
            var vm = MakeVm(path);
            vm.SelectedItem = vm.SelectedMenu!.Items[0].Children[0];

            vm.OutdentCommand.Execute(null);

            await Assert.That(vm.SelectedMenu.Items.Count).IsEqualTo(ExpectedTwoItems);
            await Assert.That(vm.SelectedMenu.Items[1].Title).IsEqualTo("Child");
            await Assert.That(vm.SelectedItem!.Parent).IsNull();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task Drop_BeforeTarget_MovesItemBeforeTarget()
    {
        var path = CreateSiteWithTwoItems();
        try
        {
            var vm = MakeVm(path);
            var dragged = vm.SelectedMenu!.Items[1];
            var target = vm.SelectedMenu.Items[0];

            vm.Drop(dragged, target, DropPosition.Before);

            await Assert.That(vm.SelectedMenu.Items[0].Title).IsEqualTo("Second");
            await Assert.That(vm.SelectedMenu.Items[1].Title).IsEqualTo("First");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task Drop_InsideTarget_MovesItemIntoTarget()
    {
        var path = CreateSiteWithTwoItems();
        try
        {
            var vm = MakeVm(path);
            var dragged = vm.SelectedMenu!.Items[1];
            var target = vm.SelectedMenu.Items[0];

            vm.Drop(dragged, target, DropPosition.Inside);

            await Assert.That(vm.SelectedMenu.Items.Count).IsEqualTo(1);
            await Assert.That(vm.SelectedMenu.Items[0].Children[0].Title).IsEqualTo("Second");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task CanSave_EmptyTitle_ReturnsFalse()
    {
        var path = CreateSiteWithMenus();
        try
        {
            var vm = MakeVm(path);
            vm.SelectedMenu!.Items[0].Title = "   ";

            await Assert.That(vm.CanSave).IsFalse();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task SaveAsync_CallsServiceWithDefinitions()
    {
        var path = CreateSiteWithMenus();
        try
        {
            var service = new FakeMenuService();
            var vm = new MenuEditorViewModel(service, new NullMenuRefProvider(), new NullInputDialog());
            vm.LoadProject(path);
            vm.Menus.Add(new MenuViewModel(new MenuDefinition("main", [])));
            vm.SelectedMenu = vm.Menus[0];
            vm.AddItemCommand.Execute(null);
            vm.SelectedItem!.Title = "About";
            vm.SelectedItem.Ref = "pages/about";

            await vm.SaveCommand.ExecuteAsync(null);

            await Assert.That(service.SavedMenus).IsNotNull();
            await Assert.That(service.SavedMenus!.Count).IsEqualTo(1);
            await Assert.That(service.SavedMenus[0].Items.Count).IsEqualTo(1);
            await Assert.That(vm.StatusMessage).IsEqualTo("Menus saved.");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static MenuEditorViewModel MakeVm(string projectPath)
    {
        var vm = new MenuEditorViewModel(
            new MenuService(),
            new NullMenuRefProvider(),
            new NullInputDialog());
        vm.LoadProject(projectPath);
        return vm;
    }

    private static string CreateSiteWithMenus()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(path, "content", "posts"));
        Directory.CreateDirectory(Path.Combine(path, "content", "pages"));

        File.WriteAllText(Path.Combine(path, "site.yaml"), """
            title: Test
            baseUrl: https://example.com
            collections:
              posts:
                name: posts
                content: content/posts
              pages:
                name: pages
                content: content/pages
            menus:
              main:
                - title: Home
                  ref: pages/home
            """);

        File.WriteAllText(Path.Combine(path, "content", "posts", "hello.md"), """
            ---
            title: Hello
            date: 2026-07-20
            ---
            Content
            """);

        return path;
    }

    private static string CreateSiteWithTwoItems()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);

        File.WriteAllText(Path.Combine(path, "site.yaml"), """
            title: Test
            baseUrl: https://example.com
            menus:
              main:
                - title: First
                  ref: pages/first
                - title: Second
                  ref: pages/second
            """);

        return path;
    }

    private static string CreateSiteWithNestedItems()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);

        File.WriteAllText(Path.Combine(path, "site.yaml"), """
            title: Test
            baseUrl: https://example.com
            menus:
              main:
                - title: Parent
                  ref: pages/parent
                  children:
                    - title: Child
                      ref: pages/child
            """);

        return path;
    }

    private sealed class NullMenuRefProvider : IMenuRefProvider
    {
        public IReadOnlyList<string> GetCollectionRefs(string projectPath) => [];

        public IReadOnlyList<string> GetItemRefs(string projectPath) => [];
    }

    private sealed class FakeMenuService : IMenuService
    {
        public IReadOnlyList<MenuDefinition>? SavedMenus { get; private set; }

        public IReadOnlyList<MenuDefinition> LoadMenus(string projectPath) => [];

        public void SaveMenus(string projectPath, IReadOnlyList<MenuDefinition> menus)
        {
            SavedMenus = menus;
        }
    }
}
