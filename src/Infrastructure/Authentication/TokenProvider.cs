using System.Security.Claims;
using System.Security.Cryptography;
using Application.Abstractions.Authentication;
using Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

internal sealed class TokenProvider(IOptions<JwtOptions> options, JwtSigningKeys signingKeys) : ITokenProvider
{
    public string Create(User user, IEnumerable<string> roles)
    {
        JwtOptions jwt = options.Value;

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(CustomClaims.MustChangePassword, user.MustChangePassword ? "true" : "false")
        ];

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwt.ExpirationInMinutes),
            SigningCredentials = signingKeys.SigningCredentials,
            Issuer = jwt.Issuer,
            Audience = jwt.Audience
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
}
