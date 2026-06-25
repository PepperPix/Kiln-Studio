namespace Kiln.Studio.Services;

public sealed record OpenedProject(
    string ProjectPath,
    string SiteTitle,
    IReadOnlyList<ContentCollectionDto> Collections);
