using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IDailyAssignmentDal : IEntityRepository<DailyAssignment>
{
    Task<PagedResult<DailyAssignmentDto>> GetAssignmentDetailsPagedAsync(DailyAssignmentFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging);

    Task<DailyAssignmentDto?> GetAssignmentDetailAsync(Guid id);
}
