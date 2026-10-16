using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfPlatformUserDal : EfEntityRepositoryBase<PlatformUser, HafizTakipDbContext>, IPlatformUserDal
{
    public EfPlatformUserDal(HafizTakipDbContext context) : base(context)
    {
    }

    public async Task<PlatformUser?> GetByEmailAsync(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await Context.PlatformUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email.ToLower() == normalized);
    }
}
