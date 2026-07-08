namespace Kiln.Studio.Services;

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

public sealed class ContentFrontmatterWriter : IContentFrontmatterWriter
{
    public bool SetDraft(string sourcePath, bool draft)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!File.Exists(sourcePath))
            throw new ContentWriteException($"File not found: {sourcePath}");

        var content = File.ReadAllText(sourcePath);
        var (fmText, body, fmType) = ParseFrontmatter(content);

        if (fmType == FrontmatterType.Toml)
            throw new ContentWriteException("TOML front matter is not supported.");

        string newContent;
        if (fmType == FrontmatterType.Yaml)
        {
            var root = ParseMapping(fmText);
            root.Children[new YamlScalarNode("draft")] = new YamlScalarNode(draft ? "true" : "false")
                { Style = ScalarStyle.Plain };

            using var sw = new StringWriter();
            new YamlStream(new YamlDocument(root)).Save(sw, assignAnchors: false);
            var emitted = NormalizeEmitted(sw.ToString());

            newContent = "---\n" + emitted + "---\n" + body;
        }
        else
        {
            newContent = $"---\ndraft: {BoolToString(draft)}\n---\n" + content;
        }

        File.WriteAllText(sourcePath, newContent);
        return draft;
    }

    public bool ToggleDraft(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!File.Exists(sourcePath))
            throw new ContentWriteException($"File not found: {sourcePath}");

        var content = File.ReadAllText(sourcePath);
        var (fmText, _, fmType) = ParseFrontmatter(content);

        if (fmType == FrontmatterType.Toml)
            throw new ContentWriteException("TOML front matter is not supported.");

        var currentDraft = false;
        if (fmType == FrontmatterType.Yaml && fmText is not null)
        {
            var root = ParseMapping(fmText);
            if (root.Children.TryGetValue(new YamlScalarNode("draft"), out var node) &&
                node is YamlScalarNode scalar &&
                bool.TryParse(scalar.Value, out var parsed))
            {
                currentDraft = parsed;
            }
        }

        return SetDraft(sourcePath, !currentDraft);
    }

    public IReadOnlyList<string> GetTaxonomyValues(string sourcePath, string taxonomyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(taxonomyName);

        if (!File.Exists(sourcePath))
            throw new ContentWriteException($"File not found: {sourcePath}");

        var content = File.ReadAllText(sourcePath);
        var (fmText, _, fmType) = ParseFrontmatter(content);

        if (fmType != FrontmatterType.Yaml || fmText is null)
            return [];

        var root = ParseMapping(fmText);
        if (!root.Children.TryGetValue(new YamlScalarNode(taxonomyName), out var node))
            return [];

        return node switch
        {
            YamlSequenceNode sequence => sequence.Children
                .OfType<YamlScalarNode>()
                .Select(n => n.Value)
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .ToList(),
            YamlScalarNode { Value: { Length: > 0 } value } => [value],
            _ => []
        };
    }

    public void SetTaxonomyValues(string sourcePath, string taxonomyName, IReadOnlyList<string> values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(taxonomyName);
        ArgumentNullException.ThrowIfNull(values);

        if (!File.Exists(sourcePath))
            throw new ContentWriteException($"File not found: {sourcePath}");

        var content = File.ReadAllText(sourcePath);
        var (fmText, body, fmType) = ParseFrontmatter(content);

        if (fmType == FrontmatterType.Toml)
            throw new ContentWriteException("TOML front matter is not supported.");

        if (fmType == FrontmatterType.None && values.Count == 0)
            return;

        var root = ParseMapping(fmText);
        var key = new YamlScalarNode(taxonomyName);

        if (values.Count == 0)
        {
            root.Children.Remove(key);
        }
        else
        {
            var sequence = new YamlSequenceNode();
            foreach (var value in values)
                sequence.Add(new YamlScalarNode(value));
            root.Children[key] = sequence;
        }

        using var sw = new StringWriter();
        new YamlStream(new YamlDocument(root)).Save(sw, assignAnchors: false);
        var emitted = NormalizeEmitted(sw.ToString());

        var newContent = "---\n" + emitted + "---\n" + body;
        File.WriteAllText(sourcePath, newContent);
    }

    private enum FrontmatterType { None, Yaml, Toml }

    private static (string? fmText, string body, FrontmatterType type) ParseFrontmatter(string content)
    {
        if (string.IsNullOrEmpty(content))
            return (null, string.Empty, FrontmatterType.None);

        var firstLine = ReadFirstLine(content);

        if (firstLine == "---")
            return ParseYamlFrontmatter(content);

        if (firstLine == "+++")
            return (null, content, FrontmatterType.Toml);

        return (null, content, FrontmatterType.None);
    }

    private static string ReadFirstLine(string content)
    {
        var newlineIndex = content.IndexOf('\n', StringComparison.Ordinal);
        return newlineIndex < 0
            ? content.TrimEnd('\r')
            : content[..newlineIndex].TrimEnd('\r');
    }

    private static (string? fmText, string body, FrontmatterType type) ParseYamlFrontmatter(string content)
    {
        var firstNewline = content.IndexOf('\n', StringComparison.Ordinal);
        if (firstNewline < 0)
            return (null, content, FrontmatterType.None);

        var startPos = firstNewline + 1;
        const int markerLen = 3;
        var closingPos = FindClosingMarker(content, startPos);

        if (closingPos < 0)
            return (null, content, FrontmatterType.None);

        var fmText = content[startPos..closingPos];
        var bodyStart = closingPos + markerLen;
        if (bodyStart < content.Length && content[bodyStart] == '\r')
            bodyStart++;
        if (bodyStart < content.Length && content[bodyStart] == '\n')
            bodyStart++;
        var body = bodyStart < content.Length ? content[bodyStart..] : string.Empty;

        return (fmText, body, FrontmatterType.Yaml);
    }

    private static int FindClosingMarker(string content, int startPos)
    {
        const int markerLen = 3;
        for (var i = startPos; i <= content.Length - markerLen; i++)
        {
            var c = content[i];
            if (c is not '-' and not '.')
                continue;

            var isDashMarker = c == '-' && content[i + 1] == '-' && content[i + 2] == '-';
            var isDotMarker = c == '.' && content[i + 1] == '.' && content[i + 2] == '.';
            var isMarker = isDashMarker || isDotMarker;
            if (!isMarker)
                continue;

            var end = content.IndexOf('\n', i);
            var line = end >= 0 ? content[i..end] : content[i..];
            if (line.TrimEnd('\r') is "---" or "...")
                return i;
        }

        return -1;
    }

    private static YamlMappingNode ParseMapping(string? fmText)
    {
        if (string.IsNullOrEmpty(fmText))
            return new YamlMappingNode();

        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(fmText));

            if (stream.Documents.Count == 0)
                return new YamlMappingNode();

            if (stream.Documents[0].RootNode is YamlMappingNode mapping)
                return mapping;

            return new YamlMappingNode();
        }
        catch (YamlException)
        {
            return new YamlMappingNode();
        }
    }

    private static string NormalizeEmitted(string yaml)
    {
        yaml = yaml.TrimEnd();

        if (yaml.EndsWith("...", StringComparison.Ordinal))
        {
            var afterLastNewline = yaml.LastIndexOf('\n');
            var lastLine = afterLastNewline >= 0 ? yaml[(afterLastNewline + 1)..] : yaml;
            if (lastLine == "...")
                yaml = afterLastNewline >= 0 ? yaml[..afterLastNewline].TrimEnd() : string.Empty;
        }

        return yaml.Length > 0 ? yaml + "\n" : "\n";
    }

    private static string BoolToString(bool value) => value ? "true" : "false";
}
