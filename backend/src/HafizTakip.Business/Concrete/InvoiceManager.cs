using HafizTakip.Core.Enums;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Business.ValidationRules.FluentValidation;
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
        var result = await _invoiceDal.GetInvoiceDetailsPagedAsync(new InvoiceFilter(), paginationParams);
        return new SuccessDataResult<PagedResult<InvoiceDto>>(result, "Faturalar listelendi.");
    }

    public async Task<IDataResult<PagedResult<InvoiceDto>>> GetByTenantIdAsync(Guid tenantId, PaginationParams paginationParams)
    {
        var result = await _invoiceDal.GetInvoiceDetailsPagedAsync(new InvoiceFilter { TenantId = tenantId }, paginationParams);
        return new SuccessDataResult<PagedResult<InvoiceDto>>(result, "Kursun faturaları listelendi.");
    }

    public async Task<IDataResult<InvoiceDto>> GetByIdAsync(Guid id)
    {
        var dto = await _invoiceDal.GetInvoiceDetailAsync(id);
        return dto == null
            ? new ErrorDataResult<InvoiceDto>("Fatura bulunamadı.", ResultErrorType.NotFound)
            : new SuccessDataResult<InvoiceDto>(dto, "Fatura getirildi.");
    }

    [ValidationAspect(typeof(CreateInvoiceValidator))]
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
            Status = InvoiceStatus.Draft
        };

        await _invoiceDal.AddAsync(invoice);
        return new SuccessResult("Fatura oluşturuldu.");
    }

    [ValidationAspect(typeof(UpdateInvoiceStatusValidator))]
    public async Task<IResult> UpdateStatusAsync(UpdateInvoiceStatusDto updateInvoiceStatusDto)
    {
        var invoice = await _invoiceDal.GetAsync(i => i.Id == updateInvoiceStatusDto.Id);
        if (invoice == null) return new ErrorResult("Fatura bulunamadı.");

        invoice.Status = updateInvoiceStatusDto.Status;
        if (updateInvoiceStatusDto.Status == InvoiceStatus.Paid)
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
