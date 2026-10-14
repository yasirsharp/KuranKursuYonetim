using System.Security.Claims;
using HafizTakip.Core.Utilities.Security;
using Microsoft.AspNetCore.Http;

namespace HafizTakip.Core.Utilities.IoC;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string? Role => Principal?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsInRoleOrAbove(string minimumRole) => RoleHierarchy.IsAtLeast(Role, minimumRole);
}
