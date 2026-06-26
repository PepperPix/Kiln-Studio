namespace Kiln.Studio.Services.Dto;

public sealed record BuildSummary(
    bool Success,
    int RenderedFiles,
    int TotalFiles,
    int SkippedDrafts,
    double DurationMs,
    string OutputDirectory,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);