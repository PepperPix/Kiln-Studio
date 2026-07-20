namespace Kiln.Studio.Services;

/// <summary>
/// A content item that references an asset (source markdown path + display title).
/// </summary>
public sealed record AssetContentReference(string SourcePath, string Title);
