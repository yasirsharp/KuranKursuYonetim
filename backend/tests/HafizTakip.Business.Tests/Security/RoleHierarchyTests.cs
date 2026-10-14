using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Security;

namespace HafizTakip.Business.Tests.Security;

public class RoleHierarchyTests
{
    [Theory]
    [InlineData(UserRole.TenantAdmin, UserRole.TenantAdmin, true)]   // ikinci yönetici atanabilir
    [InlineData(UserRole.TenantAdmin, UserRole.HeadTeacher, true)]
    [InlineData(UserRole.TenantAdmin, UserRole.Parent, true)]
    [InlineData(UserRole.HeadTeacher, UserRole.AssistantTeacher, true)]
    [InlineData(UserRole.HeadTeacher, UserRole.Parent, true)]
    [InlineData(UserRole.HeadTeacher, UserRole.HeadTeacher, false)]  // eş seviye atanamaz
    [InlineData(UserRole.HeadTeacher, UserRole.TenantAdmin, false)]  // üst seviye atanamaz
    [InlineData(UserRole.AssistantTeacher, UserRole.Parent, true)]
    [InlineData(UserRole.Parent, UserRole.Student, true)]
    [InlineData(UserRole.Parent, UserRole.Parent, false)]
    public void CanAssign_ShouldFollowHierarchy(string actor, string target, bool expected)
    {
        RoleHierarchy.CanAssign(actor, target).Should().Be(expected);
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.HeadTeacher)]
    [InlineData(UserRole.SuperAdmin)]
    public void CanAssign_SuperAdminTarget_ShouldAlwaysBeFalse(string actor)
    {
        RoleHierarchy.CanAssign(actor, UserRole.SuperAdmin).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin")]
    [InlineData("HEAD_TEACHER")]
    public void CanAssign_UnknownRoles_ShouldBeFalse(string? role)
    {
        RoleHierarchy.CanAssign(UserRole.TenantAdmin, role).Should().BeFalse();
        RoleHierarchy.CanAssign(role, UserRole.Parent).Should().BeFalse();
    }

    [Fact]
    public void IsAtLeast_ShouldCompareLevels()
    {
        RoleHierarchy.IsAtLeast(UserRole.TenantAdmin, UserRole.HeadTeacher).Should().BeTrue();
        RoleHierarchy.IsAtLeast(UserRole.HeadTeacher, UserRole.HeadTeacher).Should().BeTrue();
        RoleHierarchy.IsAtLeast(UserRole.Parent, UserRole.AssistantTeacher).Should().BeFalse();
        RoleHierarchy.IsAtLeast(null, UserRole.Student).Should().BeFalse();
    }
}
