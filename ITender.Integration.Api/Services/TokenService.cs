using System.Security.Claims;
using ITender.Integration.Api.Configuration;
using ITender.Integration.Api.Infrastructure.Crm;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ITender.Integration.Api.Services;

public interface ITokenService
{
    (string Token, int ExpiresIn) Create(CredentialRecord user);
}

public sealed class TokenService : ITokenService
{
    private readonly AuthOptions _options;
    private readonly SigningCredentials _credentials;

    public TokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
        _credentials = new SigningCredentials(AuthOptions.BuildKey(_options.SigningKey), SecurityAlgorithms.HmacSha256);
    }

    public (string Token, int ExpiresIn) Create(CredentialRecord user)
    {
        var now = DateTime.UtcNow;
        var lifetime = TimeSpan.FromMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Name, user.Username),
    };

        // One claim type ("permission"), one claim per granted permission.
        foreach (var permission in user.GrantedPermissions)
            claims.Add(new Claim("permission", permission));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            IssuedAt = now,
            Expires = now.Add(lifetime),
            SigningCredentials = _credentials
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), (int)lifetime.TotalSeconds);
    }
}
