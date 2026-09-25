using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authentication;

internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        List<string> failures = [];

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("'Jwt:Issuer' is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("'Jwt:Audience' is required.");
        }

        if (options.ExpirationInMinutes <= 0)
        {
            failures.Add("'Jwt:ExpirationInMinutes' must be greater than 0.");
        }

        using RSA? privateKey = TryImport(
            options.PrivateKey, "Jwt:PrivateKey", "PKCS#8", (rsa, bytes) => rsa.ImportPkcs8PrivateKey(bytes, out _), failures);

        using RSA? publicKey = TryImport(
            options.PublicKey, "Jwt:PublicKey", "SubjectPublicKeyInfo", (rsa, bytes) => rsa.ImportSubjectPublicKeyInfo(bytes, out _), failures);

        if (privateKey is not null && publicKey is not null && !IsSameKeyPair(privateKey, publicKey))
        {
            failures.Add("'Jwt:PublicKey' does not match 'Jwt:PrivateKey'; tokens signed by this instance would fail validation.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static RSA? TryImport(string value, string key, string format, Action<RSA, byte[]> import, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"'{key}' is required.");
            return null;
        }

        var rsa = RSA.Create();

        try
        {
            import(rsa, Convert.FromBase64String(value));
            return rsa;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException)
        {
            rsa.Dispose();
            failures.Add($"'{key}' is not a valid base64-encoded {format} DER RSA key.");
            return null;
        }
    }

    private static bool IsSameKeyPair(RSA privateKey, RSA publicKey)
    {
        RSAParameters fromPrivate = privateKey.ExportParameters(includePrivateParameters: false);
        RSAParameters fromPublic = publicKey.ExportParameters(includePrivateParameters: false);

        return fromPrivate.Modulus.AsSpan().SequenceEqual(fromPublic.Modulus) &&
            fromPrivate.Exponent.AsSpan().SequenceEqual(fromPublic.Exponent);
    }
}
