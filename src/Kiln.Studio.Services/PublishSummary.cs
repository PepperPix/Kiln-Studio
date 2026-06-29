namespace Kiln.Studio.Services;

public sealed record PublishSummary(
    bool Success,
    string Destination,
    int FileCount,
    string? Error);
