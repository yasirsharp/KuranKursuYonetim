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

public class AnnouncementManager : IAnnouncementService
{
    private readonly IAnnouncementDal _announcementDal;
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public AnnouncementManager(IAnnouncementDal announcementDal, IUserDal userDal, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _announcementDal = announcementDal;
        _userDal = userDal;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<IDataResult<PagedResult<AnnouncementDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var announcements = await _announcementDal.GetListAsync();
        
        var totalCount = announcements.Count;
        var pagedList = announcements
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<AnnouncementDto>();
        foreach (var announcement in pagedList)
        {
            var author = await _userDal.GetAsync(u => u.Id == announcement.AuthorId);
            
            dtoList.Add(new AnnouncementDto
            {
                Id = announcement.Id,
                TenantId = announcement.TenantId,
                AuthorId = announcement.AuthorId,
                AuthorName = author != null ? $"{author.FirstName} {author.LastName}" : "Bilinmiyor",
                Title = announcement.Title,
                Content = announcement.Content,
                TargetAudience = announcement.TargetAudience,
                IsPinned = announcement.IsPinned,
                PublishedAt = announcement.PublishedAt
            });
        }

        var result = new PagedResult<AnnouncementDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<AnnouncementDto>>(result, "Duyurular listelendi.");
    }

    public async Task<IDataResult<AnnouncementDto>> GetByIdAsync(Guid id)
    {
        var announcement = await _announcementDal.GetAsync(a => a.Id == id);
        if (announcement == null) return new ErrorDataResult<AnnouncementDto>("Duyuru bulunamadı.");

        var author = await _userDal.GetAsync(u => u.Id == announcement.AuthorId);

        var dto = new AnnouncementDto
        {
            Id = announcement.Id,
            TenantId = announcement.TenantId,
            AuthorId = announcement.AuthorId,
            AuthorName = author != null ? $"{author.FirstName} {author.LastName}" : "Bilinmiyor",
            Title = announcement.Title,
            Content = announcement.Content,
            TargetAudience = announcement.TargetAudience,
            IsPinned = announcement.IsPinned,
            PublishedAt = announcement.PublishedAt
        };

        return new SuccessDataResult<AnnouncementDto>(dto, "Duyuru getirildi.");
    }

    [ValidationAspect(typeof(CreateAnnouncementValidator))]
    public async Task<IResult> AddAsync(CreateAnnouncementDto createAnnouncementDto)
    {
        var announcement = new Announcement
        {
            TenantId = _tenantContext.TenantId,
            AuthorId = _currentUser.UserId ?? Guid.Empty, // Yazar her zaman duyuruyu yayınlayan kullanıcıdır
            Title = createAnnouncementDto.Title,
            Content = createAnnouncementDto.Content,
            TargetAudience = createAnnouncementDto.TargetAudience,
            IsPinned = createAnnouncementDto.IsPinned,
            PublishedAt = DateTime.UtcNow
        };

        await _announcementDal.AddAsync(announcement);
        return new SuccessResult("Duyuru yayınlandı.");
    }

    [ValidationAspect(typeof(UpdateAnnouncementValidator))]
    public async Task<IResult> UpdateAsync(UpdateAnnouncementDto updateAnnouncementDto)
    {
        var announcement = await _announcementDal.GetAsync(a => a.Id == updateAnnouncementDto.Id);
        if (announcement == null) return new ErrorResult("Duyuru bulunamadı.");

        announcement.Title = updateAnnouncementDto.Title;
        announcement.Content = updateAnnouncementDto.Content;
        announcement.TargetAudience = updateAnnouncementDto.TargetAudience;
        announcement.IsPinned = updateAnnouncementDto.IsPinned;
        announcement.UpdatedAt = DateTime.UtcNow;

        await _announcementDal.UpdateAsync(announcement);
        return new SuccessResult("Duyuru güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var announcement = await _announcementDal.GetAsync(a => a.Id == id);
        if (announcement == null) return new ErrorResult("Duyuru bulunamadı.");

        announcement.IsDeleted = true;
        announcement.DeletedAt = DateTime.UtcNow;

        await _announcementDal.UpdateAsync(announcement);
        return new SuccessResult("Duyuru silindi.");
    }
}
