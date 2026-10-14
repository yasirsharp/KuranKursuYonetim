using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Core.Utilities.IoC;

namespace HafizTakip.Business.Concrete;

public class ExamResultManager : IExamResultService
{
    private readonly IExamResultDal _examResultDal;
    private readonly IExamDal _examDal;
    private readonly IStudentDal _studentDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;
    private readonly IStudentAccessScope _accessScope;

    public ExamResultManager(IExamResultDal examResultDal, IExamDal examDal, IStudentDal studentDal, ITenantContext tenantContext, ITenantReferenceValidator references, IStudentAccessScope accessScope)
    {
        _examResultDal = examResultDal;
        _examDal = examDal;
        _studentDal = studentDal;
        _tenantContext = tenantContext;
        _references = references;
        _accessScope = accessScope;
    }

    public async Task<IDataResult<PagedResult<ExamResultDto>>> GetByExamIdAsync(Guid examId, PaginationParams paginationParams)
    {
        // Veli yalnızca kendi çocuğunun sonucunu görür (kapsam DAL sorgusuna uygulanır)
        var scopeIds = await _accessScope.GetAccessibleStudentIdsAsync();
        var result = await _examResultDal.GetExamResultDetailsPagedAsync(new ExamResultFilter { ExamId = examId }, scopeIds, paginationParams);
        return new SuccessDataResult<PagedResult<ExamResultDto>>(result, "Sınav sonuçları listelendi.");
    }

    public async Task<IDataResult<PagedResult<ExamResultDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        if (!await _accessScope.CanAccessStudentAsync(studentId))
            return new ErrorDataResult<PagedResult<ExamResultDto>>("Bu öğrencinin bilgilerine erişim yetkiniz yok.", ResultErrorType.Forbidden);

        var result = await _examResultDal.GetExamResultDetailsPagedAsync(new ExamResultFilter { StudentId = studentId }, null, paginationParams);
        return new SuccessDataResult<PagedResult<ExamResultDto>>(result, "Öğrencinin sınav sonuçları listelendi.");
    }

    public async Task<IDataResult<ExamResultDto>> GetByIdAsync(Guid id)
    {
        var dto = await _examResultDal.GetExamResultDetailAsync(id);
        if (dto == null || !await _accessScope.CanAccessStudentAsync(dto.StudentId))
            return new ErrorDataResult<ExamResultDto>("Sınav sonucu bulunamadı.", ResultErrorType.NotFound);

        return new SuccessDataResult<ExamResultDto>(dto, "Sınav sonucu getirildi.");
    }

    [ValidationAspect(typeof(CreateExamResultValidator))]
    public async Task<IResult> AddAsync(CreateExamResultDto createExamResultDto)
    {
        var referenceError = await _references.EnsureExamAsync(createExamResultDto.ExamId)
                             ?? await _references.EnsureStudentAsync(createExamResultDto.StudentId);
        if (referenceError != null) return referenceError;

        var exists = await _examResultDal.GetAsync(r => r.ExamId == createExamResultDto.ExamId && r.StudentId == createExamResultDto.StudentId);
        if (exists != null) return new ErrorResult("Bu öğrenci için bu sınava ait sonuç zaten girilmiş.", ResultErrorType.Conflict);

        var result = new ExamResult
        {
            TenantId = _tenantContext.TenantId,
            ExamId = createExamResultDto.ExamId,
            StudentId = createExamResultDto.StudentId,
            Score = createExamResultDto.Score,
            Grade = createExamResultDto.Grade,
            TeacherNotes = createExamResultDto.TeacherNotes
        };

        await _examResultDal.AddAsync(result);
        return new SuccessResult("Sınav sonucu eklendi.");
    }

    [ValidationAspect(typeof(UpdateExamResultValidator))]
    public async Task<IResult> UpdateAsync(UpdateExamResultDto updateExamResultDto)
    {
        var result = await _examResultDal.GetAsync(r => r.Id == updateExamResultDto.Id);
        if (result == null) return new ErrorResult("Sınav sonucu bulunamadı.");

        result.Score = updateExamResultDto.Score;
        result.Grade = updateExamResultDto.Grade;
        result.TeacherNotes = updateExamResultDto.TeacherNotes;
        result.UpdatedAt = DateTime.UtcNow;

        await _examResultDal.UpdateAsync(result);
        return new SuccessResult("Sınav sonucu güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var result = await _examResultDal.GetAsync(r => r.Id == id);
        if (result == null) return new ErrorResult("Sınav sonucu bulunamadı.");

        result.IsDeleted = true;
        result.DeletedAt = DateTime.UtcNow;

        await _examResultDal.UpdateAsync(result);
        return new SuccessResult("Sınav sonucu silindi.");
    }
}
