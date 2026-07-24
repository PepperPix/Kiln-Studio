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

public sealed record MenuDefinition(string Name, IReadOnlyList<MenuItemDefinition> Items);

public sealed record MenuItemDefinition
{
    public string Title { get; }
    public MenuLinkType LinkType { get; }
    public string? Ref { get; }

    // The engine schema stores menu URLs as plain strings; keep the DTO symmetric.
#pragma warning disable S3996, CA1056, CA1054
    public string? Url { get; }
#pragma warning restore S3996, CA1056, CA1054

    public bool External { get; }
    public IReadOnlyList<MenuItemDefinition> Children { get; }

    public MenuItemDefinition(
        string title,
        MenuLinkType linkType,
        string? @ref,
#pragma warning disable CA1054
        string? url,
#pragma warning restore CA1054
        bool external,
        IReadOnlyList<MenuItemDefinition> children)
    {
        Title = title;
        LinkType = linkType;
        Ref = @ref;
        Url = url;
        External = external;
        Children = children;
    }
}

public enum MenuLinkType
{
    Ref,
    Url
}
