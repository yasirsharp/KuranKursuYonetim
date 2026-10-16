using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.DataAccess.Abstract;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfTenantDal : EfEntityRepositoryBase<Tenant, HafizTakipDbContext>, ITenantDal
{
    public EfTenantDal(HafizTakipDbContext context) : base(context)
    {
    }

    public async Task<List<string>> GetTakenSlugsAsync(string baseSlug)
    {
        var prefix = baseSlug + "-";
        return await Context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Slug == baseSlug || t.Slug.StartsWith(prefix))
            .Select(t => t.Slug)
            .ToListAsync();
    }
}
