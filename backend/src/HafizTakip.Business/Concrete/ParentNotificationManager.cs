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
        var notifications = await _parentNotificationDal.GetListAsync();
        
        var totalCount = notifications.Count;
        var pagedList = notifications
            .OrderByDescending(x => x.CreatedAt)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<ParentNotificationDto>();
        foreach (var notification in pagedList)
        {
            var student = await _studentDal.GetAsync(s => s.Id == notification.StudentId);
            
            dtoList.Add(new ParentNotificationDto
            {
                Id = notification.Id,
                TenantId = notification.TenantId,
                StudentId = notification.StudentId,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
                NotificationType = notification.NotificationType,
                Title = notification.Title,
                Message = notification.Message,
                RecipientPhone = notification.RecipientPhone,
                DeliveryChannel = notification.DeliveryChannel,
                DeliveryStatus = notification.DeliveryStatus,
                SentAt = notification.SentAt,
                CreatedAt = notification.CreatedAt
            });
        }

        var result = new PagedResult<ParentNotificationDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<ParentNotificationDto>>(result, "Bildirimler listelendi.");
    }

    public async Task<IDataResult<PagedResult<ParentNotificationDto>>> GetByStudentIdAsync(Guid studentId, PaginationParams paginationParams)
    {
        var notifications = await _parentNotificationDal.GetListAsync(n => n.StudentId == studentId);
        
        var totalCount = notifications.Count;
        var pagedList = notifications
            .OrderByDescending(x => x.CreatedAt)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var student = await _studentDal.GetAsync(s => s.Id == studentId);
        var studentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor";

        var dtoList = pagedList.Select(notification => new ParentNotificationDto
        {
            Id = notification.Id,
            TenantId = notification.TenantId,
            StudentId = notification.StudentId,
            StudentName = studentName,
            NotificationType = notification.NotificationType,
            Title = notification.Title,
            Message = notification.Message,
            RecipientPhone = notification.RecipientPhone,
            DeliveryChannel = notification.DeliveryChannel,
            DeliveryStatus = notification.DeliveryStatus,
            SentAt = notification.SentAt,
            CreatedAt = notification.CreatedAt
        }).ToList();

        var result = new PagedResult<ParentNotificationDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<ParentNotificationDto>>(result, "Öğrenci bildirimleri listelendi.");
    }

    public async Task<IDataResult<ParentNotificationDto>> GetByIdAsync(Guid id)
    {
        var notification = await _parentNotificationDal.GetAsync(n => n.Id == id);
        if (notification == null) return new ErrorDataResult<ParentNotificationDto>("Bildirim bulunamadı.");

        var student = await _studentDal.GetAsync(s => s.Id == notification.StudentId);

        var dto = new ParentNotificationDto
        {
            Id = notification.Id,
            TenantId = notification.TenantId,
            StudentId = notification.StudentId,
            StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Bilinmiyor",
            NotificationType = notification.NotificationType,
            Title = notification.Title,
            Message = notification.Message,
            RecipientPhone = notification.RecipientPhone,
            DeliveryChannel = notification.DeliveryChannel,
            DeliveryStatus = notification.DeliveryStatus,
            SentAt = notification.SentAt,
            CreatedAt = notification.CreatedAt
        };

        return new SuccessDataResult<ParentNotificationDto>(dto, "Bildirim getirildi.");
    }

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
