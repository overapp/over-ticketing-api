using System.Security.Claims;
using System.Security.Cryptography;
using Application.Abstractions.Authentication;
using Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

internal sealed class TokenProvider(IConfiguration configuration) : ITokenProvider
{
    public string Create(User user, IEnumerable<string> roles)
    {
        SigningCredentials credentials = CreateSigningCredentials();

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!)
        ];

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(configuration.GetValue<int>("Jwt:ExpirationInMinutes")),
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"]
        };

        var handler = new JsonWebTokenHandler();

        string token = handler.CreateToken(tokenDescriptor);

        return token;
    }

    public string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(32);

        return Convert.ToBase64String(randomBytes);
    }

    private SigningCredentials CreateSigningCredentials()
    {
        string privateKeyBase64 = configuration["Jwt:PrivateKey"]!;

#pragma warning disable CA2000 // The RSA instance is owned by the returned RsaSecurityKey and used for the lifetime of the signing operation.
        var rsa = RSA.Create();
#pragma warning restore CA2000
        rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(privateKeyBase64), out _);

        var securityKey = new RsaSecurityKey(rsa);

        return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
    }
}
