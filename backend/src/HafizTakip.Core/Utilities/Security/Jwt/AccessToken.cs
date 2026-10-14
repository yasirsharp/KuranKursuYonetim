namespace HafizTakip.Core.Utilities.Security.Jwt;

public class AccessToken
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
}