using HafizTakip.Core.Enums;
using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfUserDal : EfEntityRepositoryBase<User, HafizTakipDbContext>, IUserDal
{
    public EfUserDal(HafizTakipDbContext context) : base(context)
    {
    }

    public async Task<List<User>> GetLoginCandidatesByPhoneAsync(string normalizedPhone)
    {
        return await Context.Users
            .IgnoreQueryFilters()
            .Where(u => u.PhoneNumber == normalizedPhone && !u.IsDeleted)
            // Onay bekleyen/reddedilen kursun yöneticisi de giriş yapar (yalnızca başvuru durumunu görür)
            .Where(u => Context.Tenants.Any(t => t.Id == u.TenantId && t.IsActive && !t.IsDeleted
                                                 && TenantStatus.SignInAllowed.Contains(t.Status)))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<User?> GetByIdAcrossTenantsAsync(Guid userId)
    {
        return await Context.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
    }

    public async Task<List<User>> GetTenantAdminsAcrossTenantsAsync(Guid tenantId)
    {
        return await Context.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Role == UserRole.TenantAdmin && !u.IsDeleted)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> HasPendingApplicationAsync(string normalizedPhone)
    {
        return await Context.Users
            .IgnoreQueryFilters()
            .Where(u => u.PhoneNumber == normalizedPhone && u.Role == UserRole.TenantAdmin && !u.IsDeleted)
            .AnyAsync(u => Context.Tenants.Any(t => t.Id == u.TenantId && !t.IsDeleted && t.Status == TenantStatus.Pending));
    }
}
