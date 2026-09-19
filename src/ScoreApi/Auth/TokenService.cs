using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ScoreApi.Auth;

public sealed class TokenService
{
    readonly JwtOptions options;
    readonly SigningCredentials credentials;
    readonly JsonWebTokenHandler handler = new();

    public TokenService(JwtOptions options)
    {
        this.options = options;
        credentials = new SigningCredentials(CreateKey(options.Key), SecurityAlgorithms.HmacSha256);
    }

    public static SymmetricSecurityKey CreateKey(string key)
    {
        var bytes = Encoding.UTF8.GetBytes(key);
        if (bytes.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (set the Jwt__Key environment variable).");
        return new SymmetricSecurityKey(bytes);
    }

    public (string Token, DateTime ExpiresAt) CreateToken(long userId, string loginId)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(options.ExpiresMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("name", loginId),
            }),
            Issuer = options.Issuer,
            Audience = options.Audience,
            Expires = expiresAt,
            SigningCredentials = credentials,
        };
        return (handler.CreateToken(descriptor), expiresAt);
    }
}
