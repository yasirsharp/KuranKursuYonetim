using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Dtos;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfInvoiceDal : EfEntityRepositoryBase<Invoice, HafizTakipDbContext>, IInvoiceDal
{
    public EfInvoiceDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<PagedResult<InvoiceDto>> GetInvoiceDetailsPagedAsync(InvoiceFilter filter, PaginationParams paging)
    {
        var invoices = Context.Invoices.AsNoTracking();
        if (filter.TenantId.HasValue)
            invoices = invoices.Where(i => i.TenantId == filter.TenantId);

        return Details(invoices.OrderByDescending(i => i.DueDate)).ToPagedResultAsync(paging);
    }

    public Task<InvoiceDto?> GetInvoiceDetailAsync(Guid id) =>
        Details(Context.Invoices.AsNoTracking().Where(i => i.Id == id)).FirstOrDefaultAsync();

    private IQueryable<InvoiceDto> Details(IQueryable<Invoice> invoices) =>
        from i in invoices
        join t in Context.Tenants on i.TenantId equals t.Id into tenants
        from t in tenants.DefaultIfEmpty()
        select new InvoiceDto
        {
            Id = i.Id,
            TenantId = i.TenantId,
            TenantName = t != null ? t.Name : QueryHelpers.Unknown,
            SubscriptionId = i.SubscriptionId,
            InvoiceNo = i.InvoiceNo,
            Amount = i.Amount,
            DueDate = i.DueDate,
            PaidDate = i.PaidDate,
            Status = i.Status
        };
}
