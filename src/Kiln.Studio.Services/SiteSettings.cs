namespace Kiln.Studio.Services;

#pragma warning disable CA1056, CA1054, S3996
public sealed record SiteSettings(
    string Title,
    string Description,
    string BaseUrl,
    string Language,
    string Theme);
#pragma warning restore CA1056, CA1054, S3996
