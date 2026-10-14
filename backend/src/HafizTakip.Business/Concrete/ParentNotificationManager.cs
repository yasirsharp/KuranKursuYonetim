using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;
using HafizTakip.Core.Utilities.IoC;

namespace HafizTakip.Business.Concrete;

public class ParentNotificationManager : IParentNotificationService
{
    private readonly IParentNotificationDal _parentNotificationDal;
    private readonly IStudentDal _studentDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;

    public ParentNotificationManager(IParentNotificationDal parentNotificationDal, IStudentDal studentDal, ITenantContext tenantContext, ITenantReferenceValidator references)
    {
        _parentNotificationDal = parentNotificationDal;
        _studentDal = studentDal;
        _tenantContext = tenantContext;
        _references = references;
    }

    public async Task<IDataResult<PagedResult<ParentNotificationDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var result = await _parentNotificationDal.GetNotificationDetailsPagedAsync(new ParentNotificationFilter(), paginationParams);
        return new SuccessDataResult<PagedResult<ParentNotificationDto>>(result, "Bildirimler listelendi.");
    }

    public async Task<IDataResult<PagedResult<ParentNotificationDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        var result = await _parentNotificationDal.GetNotificationDetailsPagedAsync(new ParentNotificationFilter { StudentId = studentId }, paginationParams);
        return new SuccessDataResult<PagedResult<ParentNotificationDto>>(result, "Öğrencinin bildirimleri listelendi.");
    }

    public async Task<IDataResult<ParentNotificationDto>> GetByIdAsync(Guid id)
    {
        var dto = await _parentNotificationDal.GetNotificationDetailAsync(id);
        return dto == null
            ? new ErrorDataResult<ParentNotificationDto>("Bildirim bulunamadı.", ResultErrorType.NotFound)
            : new SuccessDataResult<ParentNotificationDto>(dto, "Bildirim getirildi.");
    }

    [ValidationAspect(typeof(CreateParentNotificationValidator))]
    public async Task<IResult> AddAsync(CreateParentNotificationDto createParentNotificationDto)
    {
        var referenceError = await _references.EnsureStudentAsync(createParentNotificationDto.StudentId);
        if (referenceError != null) return referenceError;

        var notification = new ParentNotification
        {
            TenantId = _tenantContext.TenantId,
            StudentId = createParentNotificationDto.StudentId,
            NotificationType = createParentNotificationDto.NotificationType,
            Title = createParentNotificationDto.Title,
            Message = createParentNotificationDto.Message,
            RecipientPhone = createParentNotificationDto.RecipientPhone,
            DeliveryChannel = createParentNotificationDto.DeliveryChannel,
            DeliveryStatus = "pending"
        };

        await _parentNotificationDal.AddAsync(notification);
        return new SuccessResult("Bildirim oluşturuldu.");
    }

    public async Task<IResult> SendNotificationAsync(Guid id)
    {
        var notification = await _parentNotificationDal.GetAsync(n => n.Id == id);
        if (notification == null) return new ErrorResult("Bildirim bulunamadı.");

        // İleride burada INotificationSender (NetGSM, Twilio vb.) çalışacak.
        // Şimdilik sadece In-App simülasyonu yapıyoruz.
        
        notification.DeliveryStatus = "sent";
        notification.SentAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;

        await _parentNotificationDal.UpdateAsync(notification);
        return new SuccessResult("Bildirim gönderildi.");
    }
}
