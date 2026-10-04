using System.IdentityModel.Tokens.Jwt;

namespace HafizTakip.Core.Utilities.Security.Jwt;

public class AccessToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}

public class TokenOptions
{
    public string Audience { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public int AccessTokenExpiration { get; set; }
    public string SecurityKey { get; set; } = string.Empty;
}
