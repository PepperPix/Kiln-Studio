namespace Kiln.Studio.Services;

using System.Text;

/// <summary>
/// Slugifies uploaded asset file names (keeping the extension) so uploads never produce file names
/// with spaces or other characters that need URL-encoding in the final Markdown/HTML — the same
/// character-filtering rule ContentService already applies to post titles when creating slugs.
/// </summary>
internal static class FileNameSlugifier
{
    public static string Slugify(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

#pragma warning disable CA1308
        var lower = nameWithoutExt.ToLowerInvariant();
        var lowerExt = ext.ToLowerInvariant();
#pragma warning restore CA1308
        var sb = new StringBuilder();
        var lastWasHyphen = true;
        foreach (var c in lower)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                sb.Append('-');
                lastWasHyphen = true;
            }
        }

        if (sb.Length > 0 && sb[^1] == '-')
            sb.Length--;

        var slug = sb.Length > 0 ? sb.ToString() : "file";
        return slug + lowerExt;
    }
}
