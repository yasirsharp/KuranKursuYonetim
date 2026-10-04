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

public class UserManager : IUserService
{
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;

    public UserManager(IUserDal userDal, ITenantContext tenantContext)
    {
        _userDal = userDal;
        _tenantContext = tenantContext;
    }

    public async Task<IDataResult<PagedResult<UserDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var users = await _userDal.GetListAsync();
        
        var totalCount = users.Count;
        var pagedList = users
            .Skip((paginationParams.Page - 1) * paginationParams.PageSize)
            .Take(paginationParams.PageSize)
            .ToList();

        var dtoList = pagedList.Select(u => new UserDto
        {
            Id = u.Id,
            TenantId = u.TenantId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber,
            Email = u.Email,
            Role = u.Role
        }).ToList();

        var result = new PagedResult<UserDto>(dtoList, totalCount, paginationParams.Page, paginationParams.PageSize);
        return new SuccessDataResult<PagedResult<UserDto>>(result, "Kullanıcılar listelendi.");
    }

    public async Task<IDataResult<UserDto>> GetByIdAsync(Guid id)
    {
        var u = await _userDal.GetAsync(x => x.Id == id);
        if (u == null) return new ErrorDataResult<UserDto>("Kullanıcı bulunamadı.");

        var dto = new UserDto
        {
            Id = u.Id,
            TenantId = u.TenantId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            PhoneNumber = u.PhoneNumber,
            Email = u.Email,
            Role = u.Role
        };

        return new SuccessDataResult<UserDto>(dto, "Kullanıcı getirildi.");
    }

    [ValidationAspect(typeof(CreateUserValidator))]
    public async Task<IResult> AddAsync(CreateUserDto createUserDto)
    {
        var phone = createUserDto.PhoneNumber.Trim().Replace(" ", "");
        var existing = await _userDal.GetAsync(u => u.PhoneNumber == phone);
        if (existing != null)
        {
            return new ErrorResult("Bu telefon numarasına ait bir kullanıcı zaten mevcut.");
        }

        // TODO: İstenen rolü çağıranın yetkisine göre sunucuda sınırla; super_admin rolünü tenant içi kullanıcı oluşturma akışından atamaya izin verme.
        var user = new User
        {
            TenantId = _tenantContext.TenantId, // Otomatik olarak mevcut kiracıya eklenir
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PhoneNumber = phone,
            Email = createUserDto.Email,
            Role = createUserDto.Role
        };

        await _userDal.AddAsync(user);
        return new SuccessResult("Kullanıcı başarıyla eklendi.");
    }

    public async Task<IResult> UpdateAsync(UpdateUserDto updateUserDto)
    {
        var user = await _userDal.GetAsync(x => x.Id == updateUserDto.Id);
        if (user == null) return new ErrorResult("Kullanıcı bulunamadı.");

        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.PhoneNumber = updateUserDto.PhoneNumber.Trim().Replace(" ", "");
        user.Email = updateUserDto.Email;
        // TODO: Rol değişikliğini ayrı yetkili akışa taşı; kendini/başka kullanıcıyı super_admin yapmayı engelle.
        user.Role = updateUserDto.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await _userDal.UpdateAsync(user);
        return new SuccessResult("Kullanıcı güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var user = await _userDal.GetAsync(x => x.Id == id);
        if (user == null) return new ErrorResult("Kullanıcı bulunamadı.");

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _userDal.UpdateAsync(user); // Soft delete
        
        return new SuccessResult("Kullanıcı silindi (Soft Delete).");
    }
}
