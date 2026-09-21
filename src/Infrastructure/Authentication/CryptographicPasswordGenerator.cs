using System.Security.Cryptography;
using Application.Abstractions.Authentication;

namespace Infrastructure.Authentication;

public sealed class CryptographicPasswordGenerator : IPasswordGenerator
{
    private const string UppercaseChars = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // exclude I, O to avoid confusion
    private const string LowercaseChars = "abcdefghijkmnopqrstuvwxyz"; // exclude l
    private const string DigitChars = "23456789"; // exclude 0, 1
    private const string SpecialChars = "!@#$%^&*()-_=+[]{}";
    private const string AllChars = UppercaseChars + LowercaseChars + DigitChars + SpecialChars;

    public string Generate(int length = 16)
    {
        if (length < 8)
        {
            length = 8;
        }

        Span<char> chars = stackalloc char[length];

        // Ensure at least one uppercase, lowercase, digit, and special char to satisfy Identity requirements
        chars[0] = UppercaseChars[RandomNumberGenerator.GetInt32(UppercaseChars.Length)];
        chars[1] = LowercaseChars[RandomNumberGenerator.GetInt32(LowercaseChars.Length)];
        chars[2] = DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)];
        chars[3] = SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)];

        for (int i = 4; i < length; i++)
        {
            chars[i] = AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)];
        }

        // Fisher-Yates shuffle
        for (int i = length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }
}
