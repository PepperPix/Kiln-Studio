namespace Kiln.Studio.Services;

public sealed record ContentCollectionDto(
    string Name,
    IReadOnlyList<ContentEntry> Entries,
    string ContentDirectory);
