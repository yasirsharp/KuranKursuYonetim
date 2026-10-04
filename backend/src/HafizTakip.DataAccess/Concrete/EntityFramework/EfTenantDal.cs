using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.DataAccess.Abstract;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfTenantDal : EfEntityRepositoryBase<Tenant, HafizTakipDbContext>, ITenantDal
{
    public EfTenantDal(HafizTakipDbContext context) : base(context)
    {
    }
}
