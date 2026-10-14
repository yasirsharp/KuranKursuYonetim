using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IAnnouncementDal : IEntityRepository<Announcement>
{
    /// <summary>Yazar adıyla; sabitlenenler önce, sonra yayın tarihine göre azalan.</summary>
    Task<PagedResult<AnnouncementDto>> GetAnnouncementDetailsPagedAsync(PaginationParams paging);

    Task<AnnouncementDto?> GetAnnouncementDetailAsync(Guid id);
}
