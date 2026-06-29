namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public sealed class ContentFrontmatterWriterTests
{
    private static string CreateTempFile(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".md");
        File.WriteAllText(path, content);
        return path;
    }

    [Test]
    public async Task ToggleDraft_FromFalse_BecomesTrue()
    {
        var path = CreateTempFile("---\ndraft: false\n---\nBody content");
        try
        {
            var writer = new ContentFrontmatterWriter();
            var result = writer.ToggleDraft(path);

            await Assert.That(result).IsTrue();
            var content = await File.ReadAllTextAsync(path);
            await Assert.That(content).Contains("draft: true");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task ToggleDraft_FromTrue_BecomesFalse()
    {
        var path = CreateTempFile("---\ndraft: true\n---\nBody content");
        try
        {
            var writer = new ContentFrontmatterWriter();
            var result = writer.ToggleDraft(path);

            await Assert.That(result).IsFalse();
            var content = await File.ReadAllTextAsync(path);
            await Assert.That(content).Contains("draft: false");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task SetDraft_WithoutDraftKey_AddsDraft()
    {
        var path = CreateTempFile("---\ntitle: Test\n---\nBody");
        try
        {
            var writer = new ContentFrontmatterWriter();
            var result = writer.SetDraft(path, true);

            await Assert.That(result).IsTrue();
            var content = await File.ReadAllTextAsync(path);
            await Assert.That(content).Contains("draft: true");
            await Assert.That(content).Contains("title: Test");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task BodyRemainsByteIdentical()
    {
        const string body = "## Hello\n\nThis is a **test**.\n\n---\n\nMore content after a horizontal rule.\n";
        const string original = "---\ndraft: false\n---\n" + body;
        var path = CreateTempFile(original);
        try
        {
            var writer = new ContentFrontmatterWriter();
            writer.ToggleDraft(path);

            var content = await File.ReadAllTextAsync(path);
            var sepIndex = content.IndexOf("\n---\n", StringComparison.Ordinal);
            await Assert.That(sepIndex).IsGreaterThan(0);
            var actualBody = content[(sepIndex + 5)..];
            await Assert.That(actualBody).IsEqualTo(body);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task ValuesRemainUnchangedAfterToggle()
    {
        var path = CreateTempFile("---\ntitle: \"1.0\"\nfeatured: yes\ndate: 2024-01-01\ntags: [a, b]\ndraft: false\n---\nBody");
        try
        {
            var writer = new ContentFrontmatterWriter();
            writer.ToggleDraft(path);

            var content = await File.ReadAllTextAsync(path);
            await Assert.That(content).Contains("title: \"1.0\"");
            await Assert.That(content).Contains("featured: yes");
            await Assert.That(content).Contains("date: 2024-01-01");
            await Assert.That(content).Contains("tags:");
            await Assert.That(content).Contains("draft: true");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task NoFrontmatter_CreatesNewFrontmatter()
    {
        const string body = "# Just a header\n\nSome text.\n";
        var path = CreateTempFile(body);
        try
        {
            var writer = new ContentFrontmatterWriter();
            var result = writer.SetDraft(path, true);

            await Assert.That(result).IsTrue();
            var content = await File.ReadAllTextAsync(path);
            await Assert.That(content).IsEqualTo("---\ndraft: true\n---\n" + body);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task TomlFrontmatter_ThrowsContentWriteException()
    {
        var path = CreateTempFile("+++\ntitle = \"Test\"\n+++\nBody");
        try
        {
            var writer = new ContentFrontmatterWriter();
            await Assert.That(() => writer.ToggleDraft(path))
                .Throws<ContentWriteException>();
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task FileNotFound_ThrowsContentWriteException()
    {
        var writer = new ContentFrontmatterWriter();
        await Assert.That(() => writer.ToggleDraft("/nonexistent/file.md"))
            .Throws<ContentWriteException>();
    }

    [Test]
    public async Task Determinism_TwiceSetDraftTrue_ProducesIdenticalOutput()
    {
        const string originalContent = "---\ntitle: Test\nfeatured: yes\n---\nBody";
        var path = CreateTempFile(originalContent);
        try
        {
            var writer = new ContentFrontmatterWriter();
            writer.SetDraft(path, true);
            var firstContent = await File.ReadAllTextAsync(path);

            writer.SetDraft(path, true);
            var secondContent = await File.ReadAllTextAsync(path);

            await Assert.That(secondContent).IsEqualTo(firstContent);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
