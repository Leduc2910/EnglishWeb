using System.Text.RegularExpressions;

namespace EnglishTestWeb.Api.Application.Classes;

public static partial class ClassCodeNormalizer
{
    private static readonly Regex ValidCodePattern = ValidCodeRegex();

    public static string? Normalize(string? rawCode)
    {
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return null;
        }

        var normalized = rawCode.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        return ValidCodePattern.IsMatch(normalized) ? normalized : null;
    }

    [GeneratedRegex("^[A-Z0-9]{4,12}$")]
    private static partial Regex ValidCodeRegex();
}
