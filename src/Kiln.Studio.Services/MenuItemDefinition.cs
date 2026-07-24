namespace Kiln.Studio.Services;

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
