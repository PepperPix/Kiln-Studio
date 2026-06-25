namespace Kiln.Studio.Services;

using System.Text;

public sealed class ContentService : IContentService
{
    private const string FrontMatterDelimiter = "---";

    public ContentDocument Load(string filePath)
    {
        var content = File.ReadAllText(filePath, Encoding.UTF8)
            .Replace("\r\n", "\n", StringComparison.Ordinal);

        var (frontMatter, body) = Split(content);
        return new ContentDocument(filePath, frontMatter, body);
    }

    public void Save(string filePath, string frontMatter, string body)
    {
        string content;
        if (!string.IsNullOrEmpty(frontMatter))
            content = $"{FrontMatterDelimiter}\n{frontMatter.TrimEnd()}\n{FrontMatterDelimiter}\n\n{body}";
        else
            content = body;

        File.WriteAllText(filePath, content, Encoding.UTF8);
    }

    public string CreatePage(string contentDirectory, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Directory.CreateDirectory(contentDirectory);

        var slug = Slugify(title);
        var path = FindUniquePath(contentDirectory, slug);
        var today = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var frontMatter = $"title: {title}\ndate: {today}\ndraft: true";
        Save(path, frontMatter, "");
        return path;
    }

    private static (string frontMatter, string body) Split(string content)
    {
        if (!content.StartsWith(FrontMatterDelimiter, StringComparison.Ordinal))
            return ("", content);

        var lines = content.Split('\n');
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].TrimEnd() == FrontMatterDelimiter)
            {
                var frontMatter = string.Join("\n", lines[1..i]);
                var remaining = string.Join("\n", lines[(i + 1)..]);
                var body = remaining.TrimStart('\n');
                return (frontMatter, body);
            }
        }

        return ("", content);
    }

    private static string Slugify(string title)
    {
#pragma warning disable CA1308
        var lower = title.ToLowerInvariant();
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

        return sb.ToString();
    }

    private static string FindUniquePath(string dir, string slug)
    {
        var candidate = Path.Combine(dir, $"{slug}.md");
        if (!File.Exists(candidate))
            return candidate;

        var counter = 2;
        while (true)
        {
            candidate = Path.Combine(dir, $"{slug}-{counter}.md");
            if (!File.Exists(candidate))
                return candidate;
            counter++;
        }
    }
}
