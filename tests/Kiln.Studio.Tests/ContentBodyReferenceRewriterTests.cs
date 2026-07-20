namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public class ContentBodyReferenceRewriterTests
{
    private static ContentBodyReferenceRewriter Rewriter => new();

    [Test]
    public async Task Rewrite_BundleAsset_RewritesExactMarkdownTarget()
    {
        const string body = "![Photo](./photo.png)\n\nSome text.";

        var result = Rewriter.Rewrite(body, "./photo.png", "./renamed.png");

        await Assert.That(result).Contains("![Photo](./renamed.png)");
        await Assert.That(result).Contains("Some text.");
    }

    [Test]
    public async Task Rewrite_LibraryAsset_RewritesExactMarkdownTarget()
    {
        const string body = "![Logo](/assets/logo.png) and [Home](/).";

        var result = Rewriter.Rewrite(body, "/assets/logo.png", "/assets/logo-v2.png");

        await Assert.That(result).Contains("![Logo](/assets/logo-v2.png)");
        await Assert.That(result).Contains("[Home](/).");
    }

    [Test]
    public async Task Rewrite_LinkWithTitle_RewritesTargetButKeepsTitle()
    {
        const string body = "[Download](/assets/file.pdf \"PDF\")";

        var result = Rewriter.Rewrite(body, "/assets/file.pdf", "/assets/file-v2.pdf");

        await Assert.That(result).IsEqualTo("[Download](/assets/file-v2.pdf \"PDF\")");
    }

    [Test]
    public async Task Rewrite_FlowTextMentionOfFileName_IsNotChanged()
    {
        const string body = "photo.png is a nice name, but ![Photo](./photo.png) is the actual link.";

        var result = Rewriter.Rewrite(body, "./photo.png", "./renamed.png");

        await Assert.That(result).Contains("photo.png is a nice name");
        await Assert.That(result).Contains("![Photo](./renamed.png)");
    }

    [Test]
    public async Task Rewrite_PartialMatchInDifferentLinkTarget_IsNotChanged()
    {
        const string body = "![Other](./other-photo.png) and ![Photo](./photo.png).";

        var result = Rewriter.Rewrite(body, "./photo.png", "./renamed.png");

        await Assert.That(result).Contains("![Other](./other-photo.png)");
        await Assert.That(result).Contains("![Photo](./renamed.png)");
    }

    [Test]
    public async Task Rewrite_EmptyBodyOrPath_ReturnsOriginal()
    {
        await Assert.That(Rewriter.Rewrite("", "./a.png", "./b.png")).IsEqualTo("");
        await Assert.That(Rewriter.Rewrite("![A](./a.png)", "", "./b.png")).IsEqualTo("![A](./a.png)");
        await Assert.That(Rewriter.Rewrite("![A](./a.png)", "./a.png", "")).IsEqualTo("![A](./a.png)");
    }
}
