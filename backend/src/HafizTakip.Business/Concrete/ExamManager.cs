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

public class ExamManager : IExamService
{
    private readonly IExamDal _examDal;
    private readonly IGroupDal _groupDal;
    private readonly ITenantContext _tenantContext;

    public ExamManager(IExamDal examDal, IGroupDal groupDal, ITenantContext tenantContext)
    {
        _examDal = examDal;
        _groupDal = groupDal;
        _tenantContext = tenantContext;
    }

    public async Task<IDataResult<PagedResult<ExamDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var exams = await _examDal.GetListAsync();
        
        var totalCount = exams.Count;
        var pagedList = exams
            .OrderByDescending(x => x.ExamDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<ExamDto>();
        foreach (var exam in pagedList)
        {
            var group = await _groupDal.GetAsync(g => g.Id == exam.GroupId);
            
            dtoList.Add(new ExamDto
            {
                Id = exam.Id,
                TenantId = exam.TenantId,
                GroupId = exam.GroupId,
                GroupName = group != null ? group.Name : "Bilinmiyor",
                ExamType = exam.ExamType,
                ExamDate = exam.ExamDate,
                Title = exam.Title,
                Description = exam.Description
            });
        }

        var result = new PagedResult<ExamDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<ExamDto>>(result, "Sınavlar listelendi.");
    }

    public async Task<IDataResult<PagedResult<ExamDto>>> GetByGroupIdAsync(Guid groupId, PaginationParams paginationParams)
    {
        var exams = await _examDal.GetListAsync(e => e.GroupId == groupId);
        
        var totalCount = exams.Count;
        var pagedList = exams
            .OrderByDescending(x => x.ExamDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var group = await _groupDal.GetAsync(g => g.Id == groupId);
        var groupName = group != null ? group.Name : "Bilinmiyor";

        var dtoList = pagedList.Select(exam => new ExamDto
        {
            Id = exam.Id,
            TenantId = exam.TenantId,
            GroupId = exam.GroupId,
            GroupName = groupName,
            ExamType = exam.ExamType,
            ExamDate = exam.ExamDate,
            Title = exam.Title,
            Description = exam.Description
        }).ToList();

        var result = new PagedResult<ExamDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<ExamDto>>(result, "Sınıf sınavları listelendi.");
    }

    public async Task<IDataResult<ExamDto>> GetByIdAsync(Guid id)
    {
        var exam = await _examDal.GetAsync(e => e.Id == id);
        if (exam == null) return new ErrorDataResult<ExamDto>("Sınav bulunamadı.");

        var group = await _groupDal.GetAsync(g => g.Id == exam.GroupId);

        var dto = new ExamDto
        {
            Id = exam.Id,
            TenantId = exam.TenantId,
            GroupId = exam.GroupId,
            GroupName = group != null ? group.Name : "Bilinmiyor",
            ExamType = exam.ExamType,
            ExamDate = exam.ExamDate,
            Title = exam.Title,
            Description = exam.Description
        };

        return new SuccessDataResult<ExamDto>(dto, "Sınav getirildi.");
    }

    [ValidationAspect(typeof(CreateExamValidator))]
    public async Task<IResult> AddAsync(CreateExamDto createExamDto)
    {
        var group = await _groupDal.GetAsync(g => g.Id == createExamDto.GroupId);
        if (group == null) return new ErrorResult("Sınıf bulunamadı.");

        var exam = new Exam
        {
            TenantId = _tenantContext.TenantId,
            GroupId = createExamDto.GroupId,
            ExamType = createExamDto.ExamType,
            ExamDate = createExamDto.ExamDate.Date,
            Title = createExamDto.Title,
            Description = createExamDto.Description
        };

        await _examDal.AddAsync(exam);
        return new SuccessResult("Sınav oluşturuldu.");
    }

    [ValidationAspect(typeof(UpdateExamValidator))]
    public async Task<IResult> UpdateAsync(UpdateExamDto updateExamDto)
    {
        var exam = await _examDal.GetAsync(e => e.Id == updateExamDto.Id);
        if (exam == null) return new ErrorResult("Sınav bulunamadı.");

        exam.ExamType = updateExamDto.ExamType;
        exam.ExamDate = updateExamDto.ExamDate.Date;
        exam.Title = updateExamDto.Title;
        exam.Description = updateExamDto.Description;
        exam.UpdatedAt = DateTime.UtcNow;

        await _examDal.UpdateAsync(exam);
        return new SuccessResult("Sınav güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var exam = await _examDal.GetAsync(e => e.Id == id);
        if (exam == null) return new ErrorResult("Sınav bulunamadı.");

        exam.IsDeleted = true;
        exam.DeletedAt = DateTime.UtcNow;

        await _examDal.UpdateAsync(exam);
        return new SuccessResult("Sınav silindi.");
    }
}
