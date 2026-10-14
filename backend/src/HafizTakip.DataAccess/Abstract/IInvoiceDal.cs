using HafizTakip.Core.DataAccess;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.DataAccess.Abstract;

public interface IInvoiceDal : IEntityRepository<Invoice>
{
    Task<PagedResult<InvoiceDto>> GetInvoiceDetailsPagedAsync(InvoiceFilter filter, PaginationParams paging);

    Task<InvoiceDto?> GetInvoiceDetailAsync(Guid id);
}
