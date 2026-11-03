using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfInvitationDal : EfEntityRepositoryBase<Invitation, HafizTakipDbContext>, IInvitationDal
{
    public EfInvitationDal(HafizTakipDbContext context) : base(context)
    {
    }

    public async Task<(Invitation Invitation, Tenant Tenant)?> GetByCodeAcrossTenantsAsync(string code)
    {
        var row = await Context.Invitations
            .IgnoreQueryFilters()
            .Where(i => i.Code == code && !i.IsDeleted)
            .Join(Context.Tenants.IgnoreQueryFilters().Where(t => !t.IsDeleted),
                i => i.TenantId, t => t.Id, (i, t) => new { Invitation = i, Tenant = t })
            .AsNoTracking()
            .SingleOrDefaultAsync();
        return row == null ? null : (row.Invitation, row.Tenant);
    }

    public Task<bool> CodeExistsAsync(string code) =>
        Context.Invitations.IgnoreQueryFilters().AnyAsync(i => i.Code == code);
}
