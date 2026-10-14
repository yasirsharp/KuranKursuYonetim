using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IEtutNoteDal : IEntityRepository<EtutNote>
{
    Task<PagedResult<EtutNoteDto>> GetEtutNoteDetailsPagedAsync(EtutNoteFilter filter, IReadOnlyCollection<Guid>? scopeStudentIds, PaginationParams paging);

    Task<EtutNoteDto?> GetEtutNoteDetailAsync(Guid id);
}
