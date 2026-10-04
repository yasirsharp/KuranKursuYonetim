using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Abstract;

public interface IInvoiceService
{
    Task<IDataResult<PagedResult<InvoiceDto>>> GetAllAsync(PaginationParams paginationParams);
    Task<IDataResult<PagedResult<InvoiceDto>>> GetByTenantIdAsync(Guid tenantId, PaginationParams paginationParams);
    Task<IDataResult<InvoiceDto>> GetByIdAsync(Guid id);
    Task<IResult> AddAsync(CreateInvoiceDto createInvoiceDto);
    Task<IResult> UpdateStatusAsync(UpdateInvoiceStatusDto updateInvoiceStatusDto);
    Task<IResult> DeleteAsync(Guid id);
}
