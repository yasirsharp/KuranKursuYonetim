using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfRefreshTokenDal : EfEntityRepositoryBase<RefreshToken, HafizTakipDbContext>, IRefreshTokenDal
{
    public EfRefreshTokenDal(HafizTakipDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByHashAcrossTenantsAsync(string tokenHash)
    {
        return await Context.RefreshTokens
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash && !t.IsDeleted);
    }

    public async Task RotateAsync(RefreshToken current, RefreshToken replacement)
    {
        Context.RefreshTokens.Update(current);
        Context.RefreshTokens.Add(replacement);
        await Context.SaveChangesAsync();
    }

    public async Task RevokeAllForUserAsync(Guid userId, DateTime utcNow)
    {
        await Context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, utcNow)
                .SetProperty(t => t.UpdatedAt, utcNow));
    }
}