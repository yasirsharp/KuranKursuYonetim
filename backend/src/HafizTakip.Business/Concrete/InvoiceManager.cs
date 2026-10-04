using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class InvoiceManager : IInvoiceService
{
    private readonly IInvoiceDal _invoiceDal;
    private readonly ITenantDal _tenantDal;
    private readonly ISubscriptionDal _subscriptionDal;

    public InvoiceManager(IInvoiceDal invoiceDal, ITenantDal tenantDal, ISubscriptionDal subscriptionDal)
    {
        _invoiceDal = invoiceDal;
        _tenantDal = tenantDal;
        _subscriptionDal = subscriptionDal;
    }

    public async Task<IDataResult<PagedResult<InvoiceDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var invoices = await _invoiceDal.GetListAsync();
        
        var totalCount = invoices.Count;
        var pagedList = invoices
            .OrderByDescending(x => x.DueDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<InvoiceDto>();
        foreach (var invoice in pagedList)
        {
            var tenant = await _tenantDal.GetAsync(t => t.Id == invoice.TenantId);
            
            dtoList.Add(new InvoiceDto
            {
                Id = invoice.Id,
                TenantId = invoice.TenantId,
                TenantName = tenant != null ? tenant.Name : "Bilinmiyor",
                SubscriptionId = invoice.SubscriptionId,
                InvoiceNo = invoice.InvoiceNo,
                Amount = invoice.Amount,
                DueDate = invoice.DueDate,
                PaidDate = invoice.PaidDate,
                Status = invoice.Status
            });
        }

        var result = new PagedResult<InvoiceDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<InvoiceDto>>(result, "Faturalar listelendi.");
    }

    public async Task<IDataResult<PagedResult<InvoiceDto>>> GetByTenantIdAsync(Guid tenantId, PaginationParams paginationParams)
    {
        var invoices = await _invoiceDal.GetListAsync(i => i.TenantId == tenantId);
        
        var totalCount = invoices.Count;
        var pagedList = invoices
            .OrderByDescending(x => x.DueDate)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var tenant = await _tenantDal.GetAsync(t => t.Id == tenantId);
        var tenantName = tenant != null ? tenant.Name : "Bilinmiyor";

        var dtoList = pagedList.Select(invoice => new InvoiceDto
        {
            Id = invoice.Id,
            TenantId = invoice.TenantId,
            TenantName = tenantName,
            SubscriptionId = invoice.SubscriptionId,
            InvoiceNo = invoice.InvoiceNo,
            Amount = invoice.Amount,
            DueDate = invoice.DueDate,
            PaidDate = invoice.PaidDate,
            Status = invoice.Status
        }).ToList();

        var result = new PagedResult<InvoiceDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<InvoiceDto>>(result, "Kurs faturaları listelendi.");
    }

    public async Task<IDataResult<InvoiceDto>> GetByIdAsync(Guid id)
    {
        var invoice = await _invoiceDal.GetAsync(i => i.Id == id);
        if (invoice == null) return new ErrorDataResult<InvoiceDto>("Fatura bulunamadı.");

        var tenant = await _tenantDal.GetAsync(t => t.Id == invoice.TenantId);

        var dto = new InvoiceDto
        {
            Id = invoice.Id,
            TenantId = invoice.TenantId,
            TenantName = tenant != null ? tenant.Name : "Bilinmiyor",
            SubscriptionId = invoice.SubscriptionId,
            InvoiceNo = invoice.InvoiceNo,
            Amount = invoice.Amount,
            DueDate = invoice.DueDate,
            PaidDate = invoice.PaidDate,
            Status = invoice.Status
        };

        return new SuccessDataResult<InvoiceDto>(dto, "Fatura getirildi.");
    }

    public async Task<IResult> AddAsync(CreateInvoiceDto createInvoiceDto)
    {
        var tenant = await _tenantDal.GetAsync(t => t.Id == createInvoiceDto.TenantId);
        if (tenant == null) return new ErrorResult("Kurs bulunamadı.");

        var subscription = await _subscriptionDal.GetAsync(s => s.Id == createInvoiceDto.SubscriptionId);
        if (subscription == null) return new ErrorResult("Abonelik bulunamadı.");

        var invoice = new Invoice
        {
            TenantId = createInvoiceDto.TenantId,
            SubscriptionId = createInvoiceDto.SubscriptionId,
            InvoiceNo = createInvoiceDto.InvoiceNo,
            Amount = createInvoiceDto.Amount,
            DueDate = createInvoiceDto.DueDate.Date,
            Status = "draft"
        };

        await _invoiceDal.AddAsync(invoice);
        return new SuccessResult("Fatura oluşturuldu.");
    }

    public async Task<IResult> UpdateStatusAsync(UpdateInvoiceStatusDto updateInvoiceStatusDto)
    {
        var invoice = await _invoiceDal.GetAsync(i => i.Id == updateInvoiceStatusDto.Id);
        if (invoice == null) return new ErrorResult("Fatura bulunamadı.");

        invoice.Status = updateInvoiceStatusDto.Status;
        if (updateInvoiceStatusDto.Status == "paid")
        {
            invoice.PaidDate = updateInvoiceStatusDto.PaidDate?.Date ?? DateTime.UtcNow.Date;
        }
        invoice.UpdatedAt = DateTime.UtcNow;

        await _invoiceDal.UpdateAsync(invoice);
        return new SuccessResult("Fatura durumu güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var invoice = await _invoiceDal.GetAsync(i => i.Id == id);
        if (invoice == null) return new ErrorResult("Fatura bulunamadı.");

        invoice.IsDeleted = true;
        invoice.DeletedAt = DateTime.UtcNow;

        await _invoiceDal.UpdateAsync(invoice);
        return new SuccessResult("Fatura silindi.");
    }
}
