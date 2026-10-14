using HafizTakip.Core.Enums;

namespace HafizTakip.WebAPI.Security;

/// <summary>
/// Rol hiyerarşisine dayalı yetkilendirme policy'leri.
/// Kurs (tenant) policy'leri super_admin'i kapsamaz: super_admin yalnızca platform uçlarını kullanır.
/// </summary>
public static class AuthorizationPolicies
{
    public const string SuperAdmin = nameof(SuperAdmin);
    public const string TenantAdmin = nameof(TenantAdmin);
    public const string HeadTeacher = nameof(HeadTeacher);
    public const string Teacher = nameof(Teacher);
    public const string ParentOrAbove = nameof(ParentOrAbove);
    public const string TenantMember = nameof(TenantMember);

    public static IServiceCollection AddHafizTakipAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(SuperAdmin, p => p.RequireRole(UserRole.SuperAdmin));
            options.AddPolicy(TenantAdmin, p => p.RequireRole(UserRole.TenantAdmin));
            options.AddPolicy(HeadTeacher, p => p.RequireRole(UserRole.TenantAdmin, UserRole.HeadTeacher));
            options.AddPolicy(Teacher, p => p.RequireRole(
                UserRole.TenantAdmin, UserRole.HeadTeacher, UserRole.AssistantTeacher));
            options.AddPolicy(ParentOrAbove, p => p.RequireRole(
                UserRole.TenantAdmin, UserRole.HeadTeacher, UserRole.AssistantTeacher, UserRole.Parent));
            options.AddPolicy(TenantMember, p => p.RequireRole(
                UserRole.TenantAdmin, UserRole.HeadTeacher, UserRole.AssistantTeacher, UserRole.Parent, UserRole.Student));
        });

        return services;
    }
}
