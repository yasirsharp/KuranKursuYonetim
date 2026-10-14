using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IExamDal : IEntityRepository<Exam>
{
    Task<PagedResult<ExamDto>> GetExamDetailsPagedAsync(ExamFilter filter, PaginationParams paging);

    Task<ExamDto?> GetExamDetailAsync(Guid id);
}
