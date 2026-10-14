using HafizTakip.Business.Abstract;

namespace HafizTakip.Business.Tests.Common;

/// <summary>Testler için hazır öğrenci erişim kapsamları.</summary>
public static class AccessScopes
{
    /// <summary>Hoca ve üstü: kısıtsız erişim.</summary>
    public static IStudentAccessScope Unrestricted()
    {
        var scope = new Mock<IStudentAccessScope>();
        scope.Setup(s => s.GetAccessibleStudentIdsAsync()).ReturnsAsync((IReadOnlyCollection<Guid>?)null);
        scope.Setup(s => s.CanAccessStudentAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        return scope.Object;
    }

    /// <summary>Veli: yalnızca verilen öğrencilere erişim.</summary>
    public static IStudentAccessScope Only(params Guid[] studentIds)
    {
        var scope = new Mock<IStudentAccessScope>();
        scope.Setup(s => s.GetAccessibleStudentIdsAsync()).ReturnsAsync(studentIds);
        scope.Setup(s => s.CanAccessStudentAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => studentIds.Contains(id));
        return scope.Object;
    }
}