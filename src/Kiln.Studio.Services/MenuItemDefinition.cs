namespace Kiln.Studio.Services;

public sealed record MenuItemDefinition
{
    public string Title { get; }
    public MenuLinkType LinkType { get; }
    public string? Ref { get; }

    // The engine schema stores menu URLs as plain strings; keep the DTO symmetric.
    public string? Url { get; }

    public bool External { get; }
    public IReadOnlyList<MenuItemDefinition> Children { get; }

    public MenuItemDefinition(
        string title,
        MenuLinkType linkType,
        string? @ref,
        string? url,
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
