namespace Kiln.Studio.Services;

/// <summary>
/// Rewrites exact markdown link targets within the markdown body of content files.
/// </summary>
public interface IContentBodyReferenceRewriter
{
    /// <summary>
    /// Replaces occurrences of an exact asset path with a new path within Markdown link targets in the body text.
    /// Handles both direct matches and standard page-bundle relative references (e.g., prefixing with "./").
    /// </summary>
    string Rewrite(string body, string oldPath, string newPath);
}
