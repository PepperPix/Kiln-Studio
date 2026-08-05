namespace Kiln.Studio.Services;

public sealed record SiteSettings(
    string Title,
    string Description,
    string BaseUrl,
    string Language,
    string Theme);
