using HafizTakip.Business.Abstract;
using HafizTakip.Core.Utilities.Requests;
using HafizTakip.Business.ValidationRules.FluentValidation;
using HafizTakip.Core.Aspects.Autofac.Validation;
using HafizTakip.Core.Utilities.Helpers;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Results;
using HafizTakip.Core.Utilities.Security;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using HafizTakip.Entities.Dtos;

namespace HafizTakip.Business.Concrete;

public class UserManager : IUserService
{
    private readonly IUserDal _userDal;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;

    public UserManager(IUserDal userDal, ITenantContext tenantContext, ICurrentUser currentUser)
    {
        _userDal = userDal;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<IDataResult<PagedResult<UserDto>>> GetAllAsync(PaginationParams paginationParams)
    {
        var result = await _userDal.GetPagedAsync(
            filter: null,
            orderBy: q => q.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            selector: u => new UserDto
            {
                Id = u.Id,
                TenantId = u.TenantId,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                Email = u.Email,
                Role = u.Role
            },
            paging: paginationParams);
        return new SuccessDataResult<PagedResult<UserDto>>(result, "Kullanıcılar listelendi.");
    }

    public async Task<IDataResult<UserDto>> GetByIdAsync(Guid id)
    {
        var u = await _userDal.GetAsync(x => x.Id == id);
        if (u == null) return new ErrorDataResult<UserDto>("Kullanıcı bulunamadı.", ResultErrorType.NotFound);

        return new SuccessDataResult<UserDto>(ToDto(u), "Kullanıcı getirildi.");
    }

    [ValidationAspect(typeof(CreateUserValidator))]
    public async Task<IResult> AddAsync(CreateUserDto createUserDto)
    {
        if (!RoleHierarchy.CanAssign(_currentUser.Role, createUserDto.Role))
        {
            return new ErrorResult("Bu rolde kullanıcı oluşturma yetkiniz yok.", ResultErrorType.Forbidden);
        }

        var phone = PhoneNumberHelper.Normalize(createUserDto.PhoneNumber);
        var existing = await _userDal.GetAsync(u => u.PhoneNumber == phone);
        if (existing != null)
        {
            return new ErrorResult("Bu telefon numarasına ait bir kullanıcı zaten mevcut.", ResultErrorType.Conflict);
        }

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

    [ValidationAspect(typeof(UpdateUserValidator))]
    public async Task<IResult> UpdateAsync(UpdateUserDto updateUserDto)
    {
        var user = await _userDal.GetAsync(x => x.Id == updateUserDto.Id);
        if (user == null) return new ErrorResult("Kullanıcı bulunamadı.", ResultErrorType.NotFound);

        var isSelf = user.Id == _currentUser.UserId;
        if (isSelf)
        {
            // Kullanıcı kendi profilini düzenleyebilir ama kendi rolünü değiştiremez
            if (updateUserDto.Role != user.Role)
                return new ErrorResult("Kendi rolünüzü değiştiremezsiniz.", ResultErrorType.Forbidden);
        }
        else if (!RoleHierarchy.CanAssign(_currentUser.Role, user.Role) ||
                 !RoleHierarchy.CanAssign(_currentUser.Role, updateUserDto.Role))
        {
            return new ErrorResult("Bu kullanıcıyı düzenleme veya bu rolü atama yetkiniz yok.", ResultErrorType.Forbidden);
        }

        var phone = PhoneNumberHelper.Normalize(updateUserDto.PhoneNumber);
        if (phone != user.PhoneNumber)
        {
            var phoneOwner = await _userDal.GetAsync(u => u.PhoneNumber == phone);
            if (phoneOwner != null)
                return new ErrorResult("Bu telefon numarası başka bir kullanıcıya ait.", ResultErrorType.Conflict);
        }

        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.PhoneNumber = phone;
        user.Email = updateUserDto.Email;
        user.Role = updateUserDto.Role;
        user.UpdatedAt = DateTime.UtcNow;

        await _userDal.UpdateAsync(user);
        return new SuccessResult("Kullanıcı güncellendi.");
    }

    public async Task<IResult> DeleteAsync(Guid id)
    {
        var user = await _userDal.GetAsync(x => x.Id == id);
        if (user == null) return new ErrorResult("Kullanıcı bulunamadı.", ResultErrorType.NotFound);

        if (user.Id == _currentUser.UserId)
            return new ErrorResult("Kendi hesabınızı silemezsiniz.", ResultErrorType.Forbidden);

        if (!RoleHierarchy.CanAssign(_currentUser.Role, user.Role))
            return new ErrorResult("Bu kullanıcıyı silme yetkiniz yok.", ResultErrorType.Forbidden);

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _userDal.UpdateAsync(user); // Soft delete

        return new SuccessResult("Kullanıcı silindi (Soft Delete).");
    }

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        TenantId = u.TenantId,
        FirstName = u.FirstName,
        LastName = u.LastName,
        PhoneNumber = u.PhoneNumber,
        Email = u.Email,
        Role = u.Role
    };
}
