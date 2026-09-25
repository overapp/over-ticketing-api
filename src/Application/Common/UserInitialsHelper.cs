namespace Application.Common;

public static class UserInitialsHelper
{
    public static string GetInitials(string? firstName, string? lastName)
    {
        char? first = FirstLetter(firstName);
        char? last = FirstLetter(lastName);

        return first is null && last is null
            ? string.Empty
            : $"{first}{last}";
    }

    private static char? FirstLetter(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : char.ToUpperInvariant(value.Trim()[0]);
}
