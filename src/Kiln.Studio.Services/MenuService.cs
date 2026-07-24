namespace Kiln.Studio.Services;

using System.Text;
using YamlDotNet.RepresentationModel;

public sealed class MenuService : IMenuService
{
    public IReadOnlyList<MenuDefinition> LoadMenus(string projectPath)
    {
        var path = SiteYamlPath(projectPath);
        var yaml = File.ReadAllText(path, Encoding.UTF8);

        using var reader = new StringReader(yaml);
        var stream = new YamlStream();
        stream.Load(reader);

        var root = stream.Documents[0].RootNode as YamlMappingNode;
        if (root is null)
            return [];

        var menusNode = root.Children.FirstOrDefault(kvp =>
            kvp.Key is YamlScalarNode scalar && scalar.Value == "menus").Value;

        var menusMapping = menusNode as YamlMappingNode;
        if (menusMapping is null)
            return [];

        var result = new List<MenuDefinition>();
        foreach (var (keyNode, valueNode) in menusMapping.Children)
        {
            var name = keyNode.ToString();
            var items = ParseItems(valueNode as YamlSequenceNode);
            result.Add(new MenuDefinition(name, items));
        }

        return result;
    }

    public void SaveMenus(string projectPath, IReadOnlyList<MenuDefinition> menus)
    {
        ArgumentNullException.ThrowIfNull(menus);

        var path = SiteYamlPath(projectPath);
        var yaml = File.ReadAllText(path, Encoding.UTF8);

        using var reader = new StringReader(yaml);
        var stream = new YamlStream();
        stream.Load(reader);

        var root = stream.Documents[0].RootNode as YamlMappingNode
            ?? throw new InvalidOperationException("site.yaml root is not a mapping.");

        var menusNode = BuildMenusNode(menus);

        var existingMenusKey = root.Children
            .Select(kvp => kvp.Key)
            .OfType<YamlScalarNode>()
            .FirstOrDefault(k => k.Value == "menus");
        if (existingMenusKey is not null)
            root.Children.Remove(existingMenusKey);

        if (menus.Count > 0)
            root.Children.Add(new YamlScalarNode("menus"), menusNode);

        var builder = new StringBuilder();
        using (var writer = new StringWriter(builder))
        {
            stream.Save(writer, false);
        }

        // YamlStream emits LF; keep the file's existing line endings if possible.
        var output = builder.ToString();
        if (yaml.Contains('\r', StringComparison.Ordinal))
            output = output.Replace("\n", "\r\n", StringComparison.Ordinal);

        File.WriteAllText(path, output, Encoding.UTF8);
    }

    private static List<MenuItemDefinition> ParseItems(YamlSequenceNode? sequence)
    {
        if (sequence is null)
            return [];

        var result = new List<MenuItemDefinition>();
        foreach (var node in sequence.Children)
        {
            if (node is not YamlMappingNode mapping)
                continue;

            var title = GetScalar(mapping, "title") ?? string.Empty;
            var refValue = GetOptionalScalar(mapping, "ref");
            var url = GetOptionalScalar(mapping, "url");
            var external = GetBoolean(mapping, "external");
            var children = ParseItems(mapping.Children.TryGetValue("children", out var childNode)
                ? childNode as YamlSequenceNode
                : null);

            var linkType = string.IsNullOrWhiteSpace(refValue) ? MenuLinkType.Url : MenuLinkType.Ref;
            if (linkType == MenuLinkType.Url && string.IsNullOrWhiteSpace(url))
                linkType = MenuLinkType.Ref;

            result.Add(new MenuItemDefinition(title, linkType, refValue, url, external, children));
        }

        return result;
    }

    private static string? GetScalar(YamlMappingNode mapping, string key)
    {
        var node = mapping.Children.FirstOrDefault(kvp =>
            kvp.Key is YamlScalarNode keyNode && keyNode.Value == key).Value;

        return node is YamlScalarNode scalar ? scalar.Value : node?.ToString();
    }

    private static string? GetOptionalScalar(YamlMappingNode mapping, string key)
    {
        var node = mapping.Children.FirstOrDefault(kvp =>
            kvp.Key is YamlScalarNode keyNode && keyNode.Value == key).Value;

        if (node is YamlScalarNode scalar)
            return scalar.Value;

        return null;
    }

    private static bool GetBoolean(YamlMappingNode mapping, string key)
    {
        var node = mapping.Children.FirstOrDefault(kvp =>
            kvp.Key is YamlScalarNode keyNode && keyNode.Value == key).Value;

        if (node is YamlScalarNode scalar && bool.TryParse(scalar.Value, out var value))
            return value;

        return false;
    }

    private static YamlMappingNode BuildMenusNode(IReadOnlyList<MenuDefinition> menus)
    {
        var node = new YamlMappingNode();
        foreach (var menu in menus)
        {
            var sequence = new YamlSequenceNode();
            foreach (var item in menu.Items)
                sequence.Add(BuildItemNode(item));

            node.Add(menu.Name, sequence);
        }

        return node;
    }

    private static YamlMappingNode BuildItemNode(MenuItemDefinition item)
    {
        var node = new YamlMappingNode
        {
            { "title", new YamlScalarNode(item.Title) }
        };

        if (item.LinkType == MenuLinkType.Ref && !string.IsNullOrWhiteSpace(item.Ref))
            node.Add("ref", new YamlScalarNode(item.Ref));
        else if (!string.IsNullOrWhiteSpace(item.Url))
            node.Add("url", new YamlScalarNode(item.Url));

        if (item.External)
            node.Add("external", new YamlScalarNode("true"));

        if (item.Children.Count > 0)
        {
            var children = new YamlSequenceNode();
            foreach (var child in item.Children)
                children.Add(BuildItemNode(child));

            node.Add("children", children);
        }

        return node;
    }

    private static string SiteYamlPath(string projectPath)
    {
        var yamlPath = Path.Combine(projectPath, "site.yaml");
        if (File.Exists(yamlPath))
            return yamlPath;

        var ymlPath = Path.Combine(projectPath, "site.yml");
        if (File.Exists(ymlPath))
            return ymlPath;

        throw new FileNotFoundException($"No site.yaml found in: {projectPath}");
    }
}
