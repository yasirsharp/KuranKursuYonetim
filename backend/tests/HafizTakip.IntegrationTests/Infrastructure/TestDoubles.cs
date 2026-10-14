using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;

namespace HafizTakip.IntegrationTests.Infrastructure;

/// <summary>Her referansı geçerli sayar — veritabanı kısıtlarının son savunma hattını test etmek için.</summary>
public sealed class PermissiveReferenceValidator : ITenantReferenceValidator
{
    public Task<IResult?> EnsureStudentAsync(Guid studentId) => Task.FromResult<IResult?>(null);
    public Task<IResult?> EnsureStudentsAsync(IReadOnlyCollection<Guid> studentIds) => Task.FromResult<IResult?>(null);
    public Task<IResult?> EnsureGroupAsync(Guid groupId) => Task.FromResult<IResult?>(null);
    public Task<IResult?> EnsureExamAsync(Guid examId) => Task.FromResult<IResult?>(null);
    public Task<IResult?> EnsureTeacherAsync(Guid userId) => Task.FromResult<IResult?>(null);
    public Task<IResult?> EnsureParentAsync(Guid userId) => Task.FromResult<IResult?>(null);
}

public sealed class UnrestrictedAccessScope : IStudentAccessScope
{
    public Task<IReadOnlyCollection<Guid>?> GetAccessibleStudentIdsAsync() => Task.FromResult<IReadOnlyCollection<Guid>?>(null);
    public Task<bool> CanAccessStudentAsync(Guid studentId) => Task.FromResult(true);
}

public sealed class FixedCurrentUser : ICurrentUser
{
    public FixedCurrentUser(Guid userId, string role)
    {
        UserId = userId;
        Role = role;
    }

    public bool IsAuthenticated => true;
    public Guid? UserId { get; }
    public string? Role { get; }
    public bool IsInRoleOrAbove(string minimumRole) => HafizTakip.Core.Utilities.Security.RoleHierarchy.IsAtLeast(Role, minimumRole);
}
