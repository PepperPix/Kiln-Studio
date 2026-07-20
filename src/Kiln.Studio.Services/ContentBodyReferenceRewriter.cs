namespace Kiln.Studio.Services;

using System;
using System.Text.RegularExpressions;

public sealed class ContentBodyReferenceRewriter : IContentBodyReferenceRewriter
{
    public string Rewrite(string body, string oldPath, string newPath)
    {
        if (string.IsNullOrEmpty(body) || string.IsNullOrEmpty(oldPath) || string.IsNullOrEmpty(newPath))
            return body;

        body = RewriteExact(body, oldPath, newPath);

        if (!oldPath.StartsWith("./", StringComparison.Ordinal) && !oldPath.StartsWith("/", StringComparison.Ordinal))
        {
            body = RewriteExact(body, "./" + oldPath, "./" + newPath);
        }

        return body;
    }

    private static string RewriteExact(string body, string targetOldPath, string targetNewPath)
    {
        var escapedOldPath = Regex.Escape(targetOldPath);
        var pattern = $"(?<=\\]\\()({escapedOldPath})(?=[)\\s])";
        return Regex.Replace(body, pattern, targetNewPath);
    }
}
