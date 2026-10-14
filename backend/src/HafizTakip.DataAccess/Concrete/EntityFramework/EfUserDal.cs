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
            .Where(u => Context.Tenants.Any(t => t.Id == u.TenantId && t.IsActive && !t.IsDeleted))
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
}