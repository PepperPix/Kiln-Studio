namespace Kiln.Studio.UiTests;

using Avalonia.Media;

/// <summary>
/// Real (non-mocked) font-load test for PLAN-077: verifies that the bundled JetBrains Mono Light
/// font can be resolved to an actual glyph typeface through Avalonia's font manager. A resolving
/// typeface proves the resource URI and embedded family name are correct and that the bundled
/// asset is loadable at runtime (including headless CI environments without OS-installed fonts).
/// </summary>
public sealed class BundledCodeFontTests
{
    // The bundled font URI is intentionally hardcoded: this test proves that the exact
    // avares:// URI used by the KilnCodeFontFamily resource resolves at runtime.
#pragma warning disable S1075
    private const string BundledCodeFontUri = "avares://Kiln.Studio/Assets/Fonts/JetBrainsMono#JetBrains Mono Light";
#pragma warning restore S1075

    [Test]
    public async Task BundledJetBrainsMonoLight_ResolvesToGlyphTypeface()
    {
        var fontFamily = new FontFamily(BundledCodeFontUri);
        var typeface = new Typeface(fontFamily);

        var resolved = FontManager.Current.TryGetGlyphTypeface(typeface, out var glyphTypeface);

        await Assert.That(resolved).IsTrue();
        await Assert.That(glyphTypeface).IsNotNull();
    }
}
