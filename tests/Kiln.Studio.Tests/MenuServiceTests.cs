namespace Kiln.Studio.Tests;

using Services;

public class MenuServiceTests
{
    private const int ExpectedTwoItems = 2;
    [Test]
    public async Task LoadMenus_NoMenusBlock_ReturnsEmptyList()
    {
        var path = CreateSiteYaml("""
            title: Test
            baseUrl: https://example.com
            """);
        try
        {
            var service = new MenuService();

            var menus = service.LoadMenus(path);

            await Assert.That(menus).IsEmpty();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task LoadMenus_FlatItems_ReturnsMenuWithItems()
    {
        var path = CreateSiteYaml("""
            title: Test
            baseUrl: https://example.com
            menus:
              main:
                - title: Home
                  ref: pages/home
                - title: External
                  url: https://example.com
                  external: true
            """);
        try
        {
            var service = new MenuService();

            var menus = service.LoadMenus(path);

            await Assert.That(menus.Count).IsEqualTo(1);
            await Assert.That(menus[0].Name).IsEqualTo("main");
            await Assert.That(menus[0].Items.Count).IsEqualTo(ExpectedTwoItems);
            await Assert.That(menus[0].Items[0].Title).IsEqualTo("Home");
            await Assert.That(menus[0].Items[0].LinkType).IsEqualTo(MenuLinkType.Ref);
            await Assert.That(menus[0].Items[0].Ref).IsEqualTo("pages/home");
            await Assert.That(menus[0].Items[1].Title).IsEqualTo("External");
            await Assert.That(menus[0].Items[1].LinkType).IsEqualTo(MenuLinkType.Url);
            await Assert.That(menus[0].Items[1].Url).IsEqualTo("https://example.com");
            await Assert.That(menus[0].Items[1].External).IsTrue();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task LoadMenus_NestedItems_PreservesHierarchy()
    {
        var path = CreateSiteYaml("""
            title: Test
            baseUrl: https://example.com
            menus:
              main:
                - title: Posts
                  ref: posts/
                  children:
                    - title: Guides
                      ref: posts/guides
            """);
        try
        {
            var service = new MenuService();

            var menus = service.LoadMenus(path);

            await Assert.That(menus[0].Items[0].Children.Count).IsEqualTo(1);
            await Assert.That(menus[0].Items[0].Children[0].Title).IsEqualTo("Guides");
            await Assert.That(menus[0].Items[0].Children[0].Ref).IsEqualTo("posts/guides");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task SaveMenus_RoundTrip_PreservesOtherYaml()
    {
        var path = CreateSiteYaml("""
            title: Test
            baseUrl: https://example.com
            theme: default
            menus:
              main:
                - title: Home
                  ref: pages/home
            """);
        try
        {
            var service = new MenuService();
            var menus = new List<MenuDefinition>
            {
                new("main", new List<MenuItemDefinition>
                {
                    new("About", MenuLinkType.Ref, "pages/about", null, false, [])
                })
            };

            service.SaveMenus(path, menus);

            var yaml = await File.ReadAllTextAsync(Path.Combine(path, "site.yaml"));
            await Assert.That(yaml).Contains("title: Test");
            await Assert.That(yaml).Contains("baseUrl: https://example.com");
            await Assert.That(yaml).Contains("theme: default");
            await Assert.That(yaml).Contains("About");
            await Assert.That(yaml).DoesNotContain("Home");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    [Test]
    public async Task SaveMenus_EmptyMenus_RemovesMenusBlock()
    {
        var path = CreateSiteYaml("""
            title: Test
            menus:
              main:
                - title: Home
                  ref: pages/home
            """);
        try
        {
            var service = new MenuService();

            service.SaveMenus(path, []);

            var yaml = await File.ReadAllTextAsync(Path.Combine(path, "site.yaml"));
            await Assert.That(yaml).DoesNotContain("menus:");
            await Assert.That(yaml).Contains("title: Test");
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }

    private static string CreateSiteYaml(string yaml)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, "site.yaml"), yaml);
        return path;
    }
}
