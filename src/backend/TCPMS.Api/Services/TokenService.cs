using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using TCPMS.Api.Domain;

namespace TCPMS.Api.Services;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "TCPMS";
    public string Audience { get; set; } = "TCPMS.Client";
    public string SecretKey { get; set; } = string.Empty;
}

public sealed class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAt) CreateAdminToken(AdminUser user, IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("display_name", user.DisplayName),
            new("user_type", "admin")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        if (user.StoreId.HasValue)
        {
            claims.Add(new Claim("store_id", user.StoreId.Value.ToString()));
        }

        return (CreateToken(claims, expiresAt), expiresAt);
    }

    public (string Token, DateTime ExpiresAt) CreateMiniappToken(WxUser user)
    {
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("openid", user.OpenId),
            new Claim("display_name", user.Nickname ?? "微信用户"),
            new Claim("user_type", "miniapp")
        };
        return (CreateToken(claims, expiresAt), expiresAt);
    }

    private string CreateToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

