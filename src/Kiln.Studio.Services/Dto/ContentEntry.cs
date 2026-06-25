namespace Kiln.Studio.Services;

public sealed record ContentEntry(
    string Title,
    string SourcePath,
    bool Draft,
    DateTime? Date);
