using HafizTakip.Business.Abstract;
using HafizTakip.Core.Enums;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;

namespace HafizTakip.Business.Concrete;

public class TenantReferenceValidator : ITenantReferenceValidator
{
    private static readonly string[] TeacherRoles =
    {
        UserRole.TenantAdmin, UserRole.HeadTeacher, UserRole.AssistantTeacher
    };

    private readonly IStudentDal _studentDal;
    private readonly IGroupDal _groupDal;
    private readonly IExamDal _examDal;
    private readonly IUserDal _userDal;

    public TenantReferenceValidator(IStudentDal studentDal, IGroupDal groupDal, IExamDal examDal, IUserDal userDal)
    {
        _studentDal = studentDal;
        _groupDal = groupDal;
        _examDal = examDal;
        _userDal = userDal;
    }

    public async Task<IResult?> EnsureStudentAsync(Guid studentId) =>
        await _studentDal.AnyAsync(s => s.Id == studentId)
            ? null
            : NotFound("Öğrenci bulunamadı.");

    public async Task<IResult?> EnsureStudentsAsync(IReadOnlyCollection<Guid> studentIds)
    {
        var distinctIds = studentIds.Distinct().ToList();
        if (distinctIds.Count == 0) return null;

        var found = await _studentDal.CountAsync(s => distinctIds.Contains(s.Id));
        return found == distinctIds.Count
            ? null
            : NotFound("Listedeki öğrencilerden en az biri bulunamadı.");
    }

    public async Task<IResult?> EnsureGroupAsync(Guid groupId) =>
        await _groupDal.AnyAsync(g => g.Id == groupId)
            ? null
            : NotFound("Sınıf/grup bulunamadı.");

    public async Task<IResult?> EnsureExamAsync(Guid examId) =>
        await _examDal.AnyAsync(e => e.Id == examId)
            ? null
            : NotFound("Sınav bulunamadı.");

    public async Task<IResult?> EnsureTeacherAsync(Guid userId) =>
        await _userDal.AnyAsync(u => u.Id == userId && TeacherRoles.Contains(u.Role))
            ? null
            : NotFound("Hoca bulunamadı.");

    public async Task<IResult?> EnsureParentAsync(Guid userId) =>
        await _userDal.AnyAsync(u => u.Id == userId && u.Role == UserRole.Parent)
            ? null
            : NotFound("Veli bulunamadı.");

    private static ErrorResult NotFound(string message) => new(message, ResultErrorType.NotFound);
}
