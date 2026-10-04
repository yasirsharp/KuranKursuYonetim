using System.Security.Claims;
using HafizTakip.Core.Entities.Platform;

namespace HafizTakip.Core.Utilities.Security.Jwt;

public interface ITokenHelper
{
    AccessToken CreateToken(PlatformUser user, List<string> roles, Guid tenantId);
}
