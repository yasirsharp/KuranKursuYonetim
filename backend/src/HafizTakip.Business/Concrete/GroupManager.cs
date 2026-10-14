using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class GroupManager : IGroupService
{
    private readonly IGroupDal _groupDal;
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;
    private readonly ITenantReferenceValidator _references;

    public GroupManager(IGroupDal groupDal, IUserDal userDal, ITenantContext tenantContext, ITenantReferenceValidator references)
    {
        _groupDal = groupDal;
        _userDal = userDal;
        _tenantContext = tenantContext;
        _references = references;
    }

    public async Task<IDataResult<PagedResult<GroupDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var groups = await _groupDal.GetListAsync();
        
        var totalCount = groups.Count;
        var pagedList = groups
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = new List<GroupDto>();
        foreach (var g in pagedList)
        {
            var teacher = await _userDal.GetAsync(u => u.Id == g.TeacherId);
            dtoList.Add(new GroupDto
            {
                Id = g.Id,
                TenantId = g.TenantId,
                TeacherId = g.TeacherId,
                TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
                Name = g.Name,
                Description = g.Description
            });
        }

        var result = new PagedResult<GroupDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<GroupDto>>(result, "Sınıflar listelendi.");
    }

    public async Task<IDataResult<GroupDto>> GetByIdAsync(Guid id)
    {
        var g = await _groupDal.GetAsync(x => x.Id == id);
        if (g == null) return new ErrorDataResult<GroupDto>("Sınıf bulunamadı.");

        var teacher = await _userDal.GetAsync(u => u.Id == g.TeacherId);
        
        var dto = new GroupDto
        {
            Id = g.Id,
            TenantId = g.TenantId,
            TeacherId = g.TeacherId,
            TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Bilinmiyor",
            Name = g.Name,
            Description = g.Description
        };

        return new SuccessDataResult<GroupDto>(dto, "Sınıf getirildi.");
    }

    [ValidationAspect(typeof(CreateGroupValidator))]
    public async Task<IResult> AddAsync(CreateGroupDto createGroupDto)
    {
        var referenceError = await _references.EnsureTeacherAsync(createGroupDto.TeacherId);
        if (referenceError != null) return referenceError;

        var group = new Group
        {
            TenantId = _tenantContext.TenantId,
            TeacherId = createGroupDto.TeacherId,
            Name = createGroupDto.Name,
            Description = createGroupDto.Description
        };

        await _groupDal.AddAsync(group);
        return new SuccessResult("Sınıf başarıyla oluşturuldu.");
    }

    public async Task<IResult> UpdateAsync(UpdateGroupDto updateGroupDto)
    {
        var group = await _groupDal.GetAsync(x => x.Id == updateGroupDto.Id);
        if (group == null) return new ErrorResult("Sınıf bulunamadı.", ResultErrorType.NotFound);

        var referenceError = await _references.EnsureTeacherAsync(updateGroupDto.TeacherId);
        if (referenceError != null) return referenceError;

        group.TeacherId = updateGroupDto.TeacherId;
        group.Name = updateGroupDto.Name;
        group.Description = updateGroupDto.Description;
        group.UpdatedAt = DateTime.UtcNow;

        await _groupDal.UpdateAsync(group);
        return new SuccessResult("Sınıf güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var group = await _groupDal.GetAsync(x => x.Id == id);
        if (group == null) return new ErrorResult("Sınıf bulunamadı.");

        group.IsDeleted = true;
        group.DeletedAt = DateTime.UtcNow;
        await _groupDal.UpdateAsync(group);
        
        return new SuccessResult("Sınıf silindi.");
    }
}
