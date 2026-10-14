using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Core.Utilities.IoC;

namespace HafizTakip.Business.Concrete;

public class StudentParentManager : IStudentParentService
{
    private readonly IStudentParentDal _studentParentDal;
    private readonly IStudentDal _studentDal;
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;

    public StudentParentManager(IStudentParentDal studentParentDal, IStudentDal studentDal, IUserDal userDal, ITenantContext tenantContext, ITenantReferenceValidator references)
    {
        _studentParentDal = studentParentDal;
        _studentDal = studentDal;
        _userDal = userDal;
        _tenantContext = tenantContext;
        _references = references;
    }

    public async Task<IDataResult<List<StudentParentDto>>> GetParentsByStudentIdAsync(Guid studentId)
    {
        var relations = await _studentParentDal.GetListAsync(sp => sp.StudentId == studentId);
        var dtoList = new List<StudentParentDto>();

        foreach (var rel in relations)
        {
            var student = await _studentDal.GetAsync(s => s.Id == rel.StudentId);
            var parent = await _userDal.GetAsync(u => u.Id == rel.ParentId);

            dtoList.Add(new StudentParentDto
            {
                Id = rel.Id,
                StudentId = rel.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                ParentId = rel.ParentId,
                ParentName = parent != null ? $"{parent.FirstName} {parent.LastName}" : ""
            });
        }

        return new SuccessDataResult<List<StudentParentDto>>(dtoList);
    }

    public async Task<IDataResult<List<StudentParentDto>>> GetStudentsByParentIdAsync(Guid parentId)
    {
        var relations = await _studentParentDal.GetListAsync(sp => sp.ParentId == parentId);
        var dtoList = new List<StudentParentDto>();

        foreach (var rel in relations)
        {
            var student = await _studentDal.GetAsync(s => s.Id == rel.StudentId);
            var parent = await _userDal.GetAsync(u => u.Id == rel.ParentId);

            dtoList.Add(new StudentParentDto
            {
                Id = rel.Id,
                StudentId = rel.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "",
                ParentId = rel.ParentId,
                ParentName = parent != null ? $"{parent.FirstName} {parent.LastName}" : ""
            });
        }

        return new SuccessDataResult<List<StudentParentDto>>(dtoList);
    }

    [ValidationAspect(typeof(CreateStudentParentValidator))]
    public async Task<IResult> AddAsync(CreateStudentParentDto dto)
    {
        var referenceError = await _references.EnsureStudentAsync(dto.StudentId)
                             ?? await _references.EnsureParentAsync(dto.ParentId);
        if (referenceError != null) return referenceError;

        var exists = await _studentParentDal.GetAsync(sp => sp.StudentId == dto.StudentId && sp.ParentId == dto.ParentId);
        if (exists != null) return new ErrorResult("Bu eşleşme zaten mevcut.", ResultErrorType.Conflict);

        var studentParent = new StudentParent
        {
            TenantId = _tenantContext.TenantId,
            StudentId = dto.StudentId,
            ParentId = dto.ParentId
        };

        await _studentParentDal.AddAsync(studentParent);
        return new SuccessResult("Veli-Öğrenci eşleşmesi eklendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var relation = await _studentParentDal.GetAsync(sp => sp.Id == id);
        if (relation == null) return new ErrorResult("Kayıt bulunamadı.");

        relation.IsDeleted = true;
        relation.DeletedAt = DateTime.UtcNow;

        await _studentParentDal.UpdateAsync(relation);
        return new SuccessResult("Veli eşleşmesi kaldırıldı.");
    }
}
