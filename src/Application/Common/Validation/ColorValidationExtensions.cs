using System.Text.RegularExpressions;
using FluentValidation;

namespace Application.Common.Validation;

public static partial class ColorValidationExtensions
{
    private static readonly Regex HexColorRegex = MyRegex();

    public static IRuleBuilderOptions<T, string?> ValidCssHexColor<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(color => string.IsNullOrWhiteSpace(color) || HexColorRegex.IsMatch(color))
            .WithMessage("'{PropertyName}' must be a valid CSS hex color code (e.g. #FFF, #FFFFFF, or #FFFFFFFF).");
    }

    [GeneratedRegex(@"^#([A-Fa-f0-9]{3}|[A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$")]
    private static partial Regex MyRegex();
}
