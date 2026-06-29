namespace Kiln.Studio.Tests;

using Kiln.Studio.Services;

public sealed class LanguageCodeTests
{
    [Test]
    public async Task IsValid_En_ReturnsTrue()
    {
        await Assert.That(LanguageCode.IsValid("en")).IsTrue();
    }

    [Test]
    public async Task IsValid_DeDe_ReturnsTrue()
    {
        await Assert.That(LanguageCode.IsValid("de-DE")).IsTrue();
    }

    [Test]
    public async Task IsValid_Fr_ReturnsTrue()
    {
        await Assert.That(LanguageCode.IsValid("fr")).IsTrue();
    }

    [Test]
    public async Task IsValid_Empty_ReturnsTrue()
    {
        await Assert.That(LanguageCode.IsValid(string.Empty)).IsTrue();
    }

    [Test]
    public async Task IsValid_Whitespace_ReturnsTrue()
    {
        await Assert.That(LanguageCode.IsValid("   ")).IsTrue();
    }

    [Test]
    public async Task IsValid_Null_ReturnsTrue()
    {
        await Assert.That(LanguageCode.IsValid(null)).IsTrue();
    }

    [Test]
    public async Task IsValid_EnglishWord_ReturnsFalse()
    {
        await Assert.That(LanguageCode.IsValid("english")).IsFalse();
    }

    [Test]
    public async Task IsValid_Numeric_ReturnsFalse()
    {
        await Assert.That(LanguageCode.IsValid("123")).IsFalse();
    }

    [Test]
    public async Task IsValid_SpecialChars_ReturnsFalse()
    {
        await Assert.That(LanguageCode.IsValid("e!")).IsFalse();
    }

    [Test]
    public async Task IsValid_UnknownTwoLetter_ReturnsFalse()
    {
        await Assert.That(LanguageCode.IsValid("zz")).IsFalse();
    }
}
