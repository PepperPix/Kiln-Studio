namespace Kiln.Studio.Services;

using System.Globalization;

public static class LanguageCode
{
    private static readonly HashSet<string> ValidCultureNames = CultureInfo
        .GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures)
        .Select(static c => c.Name)
        .Where(static name => name.Length > 0)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return true;

        return ValidCultureNames.Contains(code);
    }
}
