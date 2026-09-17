using System.Security.Cryptography;
using Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Authentication;

/// <summary>
/// Custom PBKDF2 password hasher wired into ASP.NET Core Identity's <see cref="IPasswordHasher{TUser}"/>,
/// so <see cref="UserManager{TUser}"/> uses it transparently.
/// </summary>
internal sealed class PasswordHasher : IPasswordHasher<User>
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 500000;

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public string HashPassword(User user, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        return $"{Convert.ToHexString(hash)}-{Convert.ToHexString(salt)}";
    }

    public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
    {
        string[] parts = hashedPassword.Split('-');

        if (parts.Length != 2)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] hash = Convert.FromHexString(parts[0]);
        byte[] salt = Convert.FromHexString(parts[1]);

        byte[] inputHash = Rfc2898DeriveBytes.Pbkdf2(providedPassword, salt, Iterations, Algorithm, HashSize);

        return CryptographicOperations.FixedTimeEquals(hash, inputHash)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }
}
