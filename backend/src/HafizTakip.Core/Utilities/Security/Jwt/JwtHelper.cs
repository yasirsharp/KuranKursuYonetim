using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Extensions;
using HafizTakip.Core.Utilities.Security.Encryption;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HafizTakip.Core.Utilities.Security.Jwt;

public class JwtHelper : ITokenHelper
{
    public IConfiguration Configuration { get; }
    private readonly TokenOptions _tokenOptions;
    private DateTime _accessTokenExpiration;

    public JwtHelper(IConfiguration configuration)
    {
        Configuration = configuration;
        _tokenOptions = Configuration.GetSection("TokenOptions").Get<TokenOptions>() 
                        ?? throw new ArgumentNullException("TokenOptions not found in configuration.");
    }

    public AccessToken CreateToken(PlatformUser user, List<string> roles, Guid tenantId)
    {
        _accessTokenExpiration = DateTime.UtcNow.AddMinutes(_tokenOptions.AccessTokenExpiration);

        var securityKey = SecurityKeyHelper.CreateSecurityKey(_tokenOptions.SecurityKey);

        var jwt = new JwtSecurityToken(
            issuer: _tokenOptions.Issuer,
            audience: _tokenOptions.Audience,
            claims: SetClaims(user, roles, tenantId),
            notBefore: DateTime.UtcNow,
            expires: _accessTokenExpiration,
            signingCredentials: SigningCredentialsHelper.CreateSigningCredentials(securityKey)
        );

        return new AccessToken
        {
            Token = new JwtSecurityTokenHandler().WriteToken(jwt),
            Expiration = _accessTokenExpiration
        };
    }

    private IEnumerable<Claim> SetClaims(PlatformUser user, List<string> roles, Guid tenantId)
    {
        var claims = new List<Claim>();
        claims.AddNameIdentifier(user.Id.ToString());
        claims.AddEmail(user.Email);
        
        if (tenantId != Guid.Empty)
        {
            claims.AddTenantId(tenantId.ToString());
        }

        if (roles != null && roles.Any())
        {
            claims.AddRoles(roles.ToArray());
        }

        return claims;
    }
}
