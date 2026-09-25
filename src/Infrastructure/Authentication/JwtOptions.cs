namespace Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Base64-encoded PKCS#8 DER RSA private key.</summary>
    public string PrivateKey { get; set; } = string.Empty;

    /// <summary>Base64-encoded X.509 SubjectPublicKeyInfo DER RSA public key.</summary>
    public string PublicKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpirationInMinutes { get; set; }
}
