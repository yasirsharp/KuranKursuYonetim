using HafizTakip.Core.Enums;

namespace HafizTakip.Core.Utilities.Security;

/// <summary>
/// Rol hiyerarşisi: super_admin > tenant_admin > head_teacher > assistant_teacher > parent > student.
/// super_admin platform rolüdür; kurs (tenant) içindeki kullanıcılara asla atanamaz.
/// </summary>
public static class RoleHierarchy
{
    private static readonly Dictionary<string, int> Levels = new()
    {
        [UserRole.SuperAdmin] = 100,
        [UserRole.TenantAdmin] = 80,
        [UserRole.HeadTeacher] = 60,
        [UserRole.AssistantTeacher] = 40,
        [UserRole.Parent] = 20,
        [UserRole.Student] = 10
    };

    /// <summary>Kurs içindeki kullanıcılara atanabilecek roller.</summary>
    public static readonly IReadOnlyCollection<string> TenantRoles = new[]
    {
        UserRole.TenantAdmin,
        UserRole.HeadTeacher,
        UserRole.AssistantTeacher,
        UserRole.Parent,
        UserRole.Student
    };

    /// <summary>Bilinmeyen rol için 0 döner.</summary>
    public static int GetLevel(string? role) =>
        role != null && Levels.TryGetValue(role, out var level) ? level : 0;

    public static bool IsTenantRole(string? role) =>
        role != null && TenantRoles.Contains(role);

    /// <summary>role, minimumRole ile aynı veya daha yetkili mi?</summary>
    public static bool IsAtLeast(string? role, string minimumRole) =>
        GetLevel(role) > 0 && GetLevel(role) >= GetLevel(minimumRole);

    /// <summary>
    /// actorRole'e sahip kullanıcı, bir kullanıcıya targetRole atayabilir mi?
    /// Kurs yöneticisi kendi seviyesine kadar (ikinci bir yönetici dahil) atayabilir;
    /// diğer roller yalnızca kendilerinden düşük rolleri atayabilir.
    /// </summary>
    public static bool CanAssign(string? actorRole, string? targetRole)
    {
        if (!IsTenantRole(actorRole) || !IsTenantRole(targetRole))
            return false;

        var actorLevel = GetLevel(actorRole);
        var targetLevel = GetLevel(targetRole);

        return actorRole == UserRole.TenantAdmin
            ? targetLevel <= actorLevel
            : targetLevel < actorLevel;
    }
}
