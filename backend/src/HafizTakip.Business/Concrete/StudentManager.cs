using HafizTakip.Core.Enums;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class StudentManager : IStudentService
{
    private readonly IStudentDal _studentDal;
    private readonly IGroupDal _groupDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;
    private readonly IStudentAccessScope _accessScope;

    public StudentManager(IStudentDal studentDal, IGroupDal groupDal, ITenantContext tenantContext, ITenantReferenceValidator references, IStudentAccessScope accessScope)
    {
        _studentDal = studentDal;
        _groupDal = groupDal;
        _tenantContext = tenantContext;
        _references = references;
        _accessScope = accessScope;
    }

    public async Task<IDataResult<PagedResult<StudentDto>>> GetAllAsync(StudentFilter filter, PaginationParams paginationParams)
    {
        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var result = await _studentDal.GetStudentDetailsPagedAsync(filter, scopeIds, paginationParams);
        return new SuccessDataResult<PagedResult<StudentDto>>(result, "Öğrenciler listelendi.");
    }

    public async Task<IDataResult<StudentDto>> GetByIdAsync(Guid id)
    {
        if (!await _accessScope.CanAccessStudentAsync(id))
            return new ErrorDataResult<StudentDto>("Öğrenci bulunamadı.", ResultErrorType.NotFound);

        var dto = await _studentDal.GetStudentDetailAsync(id);
        return dto == null
            ? new ErrorDataResult<StudentDto>("Öğrenci bulunamadı.", ResultErrorType.NotFound)
            : new SuccessDataResult<StudentDto>(dto, "Öğrenci detayı getirildi.");
    }

    [ValidationAspect(typeof(CreateStudentValidator))]
    public async Task<IResult> AddAsync(CreateStudentDto createStudentDto)
    {
        if (createStudentDto.GroupId.HasValue)
        {
            var group = await _groupDal.GetAsync(g => g.Id == createStudentDto.GroupId.Value);
            if (group == null) return new ErrorResult("Belirtilen sınıf bulunamadı.");
        }

        var student = new Student
        {
            TenantId = _tenantContext.TenantId,
            GroupId = createStudentDto.GroupId,
            FirstName = createStudentDto.FirstName,
            LastName = createStudentDto.LastName,
            IdentificationNumber = createStudentDto.IdentificationNumber,
            Status = StudentStatus.Active,
            CurrentPage = 0,
            CompletedPages = 0,
            IsHafiz = false
        };

        await _studentDal.AddAsync(student);
        return new SuccessResult("Öğrenci başarıyla eklendi.");
    }

    [ValidationAspect(typeof(UpdateStudentValidator))]
    public async Task<IResult> UpdateAsync(UpdateStudentDto updateStudentDto)
    {
        var student = await _studentDal.GetAsync(x => x.Id == updateStudentDto.Id);
        if (student == null) return new ErrorResult("Öğrenci bulunamadı.");

        if (updateStudentDto.GroupId is { } groupId && groupId != student.GroupId)
        {
            var referenceError = await _references.EnsureGroupAsync(groupId);
            if (referenceError != null) return referenceError;
        }

        student.GroupId = updateStudentDto.GroupId;
        student.FirstName = updateStudentDto.FirstName;
        student.LastName = updateStudentDto.LastName;
        student.IdentificationNumber = updateStudentDto.IdentificationNumber;
        student.Status = updateStudentDto.Status;
        student.UpdatedAt = DateTime.UtcNow;

        await _studentDal.UpdateAsync(student);
        return new SuccessResult("Öğrenci güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var student = await _studentDal.GetAsync(x => x.Id == id);
        if (student == null) return new ErrorResult("Öğrenci bulunamadı.");

        student.IsDeleted = true;
        student.DeletedAt = DateTime.UtcNow;
        await _studentDal.UpdateAsync(student);
        
        return new SuccessResult("Öğrenci silindi.");
    }
}
