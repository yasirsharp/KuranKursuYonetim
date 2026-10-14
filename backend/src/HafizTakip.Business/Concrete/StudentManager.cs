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

    public async Task<IDataResult<PagedResult<StudentDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        // TODO: Sayfalama ve grup adlarını veritabanı sorgusuna taşı; mevcut akış tüm öğrencileri belleğe alıp grup başına ek sorgu çalıştırıyor.
        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var students = scopeIds == null
            ? await _studentDal.GetListAsync()
            : await _studentDal.GetListAsync(s => scopeIds.Contains(s.Id));
        
        var totalCount = students.Count;
        var pagedList = students
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<StudentDto>();
        foreach (var s in pagedList)
        {
            string groupName = "Sınıfsız";
            if (s.GroupId.HasValue)
            {
                var group = await _groupDal.GetAsync(g => g.Id == s.GroupId.Value);
                if (group != null) groupName = group.Name;
            }

            dtoList.Add(new StudentDto
            {
                Id = s.Id,
                TenantId = s.TenantId,
                GroupId = s.GroupId,
                GroupName = groupName,
                FirstName = s.FirstName,
                LastName = s.LastName,
                IdentificationNumber = s.IdentificationNumber,
                CurrentPage = s.CurrentPage,
                CompletedPages = s.CompletedPages,
                IsHafiz = s.IsHafiz,
                Status = s.Status
            });
        }

        var result = new PagedResult<StudentDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<StudentDto>>(result, "Öğrenciler listelendi.");
    }

    public async Task<IDataResult<StudentDto>> GetByIdAsync(Guid id)
    {
        var s = await _studentDal.GetAsync(x => x.Id == id);
        if (s == null || !await _accessScope.CanAccessStudentAsync(s.Id))
            return new ErrorDataResult<StudentDto>("Öğrenci bulunamadı.", ResultErrorType.NotFound);

        string groupName = "Sınıfsız";
        if (s.GroupId.HasValue)
        {
            var group = await _groupDal.GetAsync(g => g.Id == s.GroupId.Value);
            if (group != null) groupName = group.Name;
        }

        var dto = new StudentDto
        {
            Id = s.Id,
            TenantId = s.TenantId,
            GroupId = s.GroupId,
            GroupName = groupName,
            FirstName = s.FirstName,
            LastName = s.LastName,
            IdentificationNumber = s.IdentificationNumber,
            CurrentPage = s.CurrentPage,
            CompletedPages = s.CompletedPages,
            IsHafiz = s.IsHafiz,
            Status = s.Status
        };

        return new SuccessDataResult<StudentDto>(dto, "Öğrenci getirildi.");
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
            Status = "active",
            CurrentPage = 0,
            CompletedPages = 0,
            IsHafiz = false
        };

        await _studentDal.AddAsync(student);
        return new SuccessResult("Öğrenci başarıyla eklendi.");
    }

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
