using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Common;

public static partial class SlugHelper
{
    [GeneratedRegex(@"[^a-z0-9\s-]", RegexOptions.Compiled)]
    private static partial Regex RemoveInvalidCharsRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex MultipleSpacesRegex();

    [GeneratedRegex(@"-+", RegexOptions.Compiled)]
    private static partial Regex MultipleHyphensRegex();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "URL slugs must be lowercase")]
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        string normalized = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        string clean = sb.ToString().Normalize(NormalizationForm.FormC);
        clean = RemoveInvalidCharsRegex().Replace(clean, " ");
        clean = MultipleSpacesRegex().Replace(clean, "-");
        clean = MultipleHyphensRegex().Replace(clean, "-").Trim('-');

        return clean;
    }
}
