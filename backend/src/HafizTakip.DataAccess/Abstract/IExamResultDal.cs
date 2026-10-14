using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IExamResultDal : IEntityRepository<ExamResult>
{
    Task<PagedResult<ExamResultDto>> GetExamResultDetailsPagedAsync(ExamResultFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging);

    Task<ExamResultDto?> GetExamResultDetailAsync(Guid id);
}
