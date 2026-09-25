using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

/// <summary>
/// Loads both halves of the JWT RSA key pair once, from the same validated <see cref="JwtOptions"/>,
/// so the key used to sign tokens and the key used to validate them always come from the same snapshot.
/// </summary>
internal sealed class JwtSigningKeys : IDisposable
{
    private readonly RSA _privateKey;
    private readonly RSA _publicKey;

    public JwtSigningKeys(IOptions<JwtOptions> options)
    {
        JwtOptions jwt = options.Value;

        _privateKey = RSA.Create();
        _privateKey.ImportPkcs8PrivateKey(Convert.FromBase64String(jwt.PrivateKey), out _);

        _publicKey = RSA.Create();
        _publicKey.ImportSubjectPublicKeyInfo(Convert.FromBase64String(jwt.PublicKey), out _);

        SigningCredentials = new SigningCredentials(new RsaSecurityKey(_privateKey), SecurityAlgorithms.RsaSha256);
        ValidationKey = new RsaSecurityKey(_publicKey);
    }

    public SigningCredentials SigningCredentials { get; }

    public RsaSecurityKey ValidationKey { get; }

    public void Dispose()
    {
        _privateKey.Dispose();
        _publicKey.Dispose();
    }
}
