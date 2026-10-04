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

    public ExamResultManager(IExamResultDal examResultDal, IExamDal examDal, IStudentDal studentDal, ITenantContext tenantContext)
    {
        _examResultDal = examResultDal;
        _examDal = examDal;
        _studentDal = studentDal;
        _tenantContext = tenantContext;
    }

    public async Task<IDataResult<PagedResult<ExamResultDto>>> GetByExamIdAsync(Guid examId, PaginationParams paginationParams)
    {
        var results = await _examResultDal.GetListAsync(r => r.ExamId == examId);
        
        var totalCount = results.Count;
        var pagedList = results
            .OrderByDescending(x => x.Score)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var exam = await _examDal.GetAsync(e => e.Id == examId);
        var examTitle = exam != null ? exam.Title : "Bilinmiyor";

        var dtoList = new List<ExamResultDto>();
        foreach (var result in pagedList)
        {
            var student = await _studentDal.GetAsync(s => s.Id == result.StudentId);
            
            dtoList.Add(new ExamResultDto
            {
                Id = result.Id,
                TenantId = result.TenantId,
                ExamId = result.ExamId,
                ExamTitle = examTitle,
                StudentId = result.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
                Score = result.Score,
                Grade = result.Grade,
                TeacherNotes = result.TeacherNotes
            });
        }

        var pagedResult = new PagedResult<ExamResultDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<ExamResultDto>>(pagedResult, "Sınav sonuçları listelendi.");
    }

    public async Task<IDataResult<PagedResult<ExamResultDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        var results = await _examResultDal.GetListAsync(r => r.StudentId == studentId);
        
        var totalCount = results.Count;
        var pagedList = results
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var student = await _studentDal.GetAsync(s => s.Id == studentId);
        var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor";

        var dtoList = new List<ExamResultDto>();
        foreach (var result in pagedList)
        {
            var exam = await _examDal.GetAsync(e => e.Id == result.ExamId);
            
            dtoList.Add(new ExamResultDto
            {
                Id = result.Id,
                TenantId = result.TenantId,
                ExamId = result.ExamId,
                ExamTitle = exam != null ? exam.Title : "Bilinmiyor",
                StudentId = result.StudentId,
                StudentName = studentName,
                Score = result.Score,
                Grade = result.Grade,
                TeacherNotes = result.TeacherNotes
            });
        }

        var pagedResult = new PagedResult<ExamResultDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<ExamResultDto>>(pagedResult, "Öğrenci sınav sonuçları listelendi.");
    }

    public async Task<IDataResult<ExamResultDto>> GetByIdAsync(Guid id)
    {
        var result = await _examResultDal.GetAsync(r => r.Id == id);
        if (result == null) return new ErrorDataResult<ExamResultDto>("Sınav sonucu bulunamadı.");

        var exam = await _examDal.GetAsync(e => e.Id == result.ExamId);
        var student = await _studentDal.GetAsync(s => s.Id == result.StudentId);

        var dto = new ExamResultDto
        {
            Id = result.Id,
            TenantId = result.TenantId,
            ExamId = result.ExamId,
            ExamTitle = exam != null ? exam.Title : "Bilinmiyor",
            StudentId = result.StudentId,
            StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
            Score = result.Score,
            Grade = result.Grade,
            TeacherNotes = result.TeacherNotes
        };

        return new SuccessDataResult<ExamResultDto>(dto, "Sınav sonucu getirildi.");
    }

    public async Task<IResult> AddAsync(CreateExamResultDto createExamResultDto)
    {
        var exists = await _examResultDal.GetAsync(r => r.ExamId == createExamResultDto.ExamId && r.StudentId == createExamResultDto.StudentId);
        if (exists != null) return new ErrorResult("Bu öğrenci için bu sınava ait sonuç zaten girilmiş.");

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
