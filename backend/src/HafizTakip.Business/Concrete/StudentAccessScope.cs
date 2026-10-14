using HafizTakip.Business.Abstract;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.DataAccess.Abstract;

namespace HafizTakip.Business.Concrete;

public class StudentAccessScope : IStudentAccessScope
{
    private readonly ICurrentUser _currentUser;
    private readonly IStudentParentDal _studentParentDal;

    // İstek başına (lifetime scope) bir kez hesaplanır
    private IReadOnlyCollection<Guid>? _accessibleIds;
    private bool _resolved;

    public StudentAccessScope(ICurrentUser currentUser, IStudentParentDal studentParentDal)
    {
        _currentUser = currentUser;
        _studentParentDal = studentParentDal;
    }

    public async Task<IReadOnlyCollection<Guid>?> GetAccessibleStudentIdsAsync()
    {
        if (_resolved) return _accessibleIds;

        if (_currentUser.IsInRoleOrAbove(UserRole.AssistantTeacher))
        {
            _accessibleIds = null; // kurs içi tüm öğrenciler
        }
        else if (_currentUser.Role == UserRole.Parent && _currentUser.UserId is { } parentId)
        {
            var relations = await _studentParentDal.GetListAsync(sp => sp.ParentId == parentId);
            _accessibleIds = relations.Select(sp => sp.StudentId).Distinct().ToList();
        }
        else
        {
            // Öğrenci rolü ve tanımsız roller için öğrenci verisi kapalı (öğrenci portalı ileri aşama)
            _accessibleIds = Array.Empty<Guid>();
        }

        _resolved = true;
        return _accessibleIds;
    }

    public async Task<bool> CanAccessStudentAsync(Guid studentId)
    {
        var ids = await GetAccessibleStudentIdsAsync();
        return ids == null || ids.Contains(studentId);
    }
}
