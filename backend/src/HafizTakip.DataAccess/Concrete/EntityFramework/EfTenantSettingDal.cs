using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfTenantSettingDal : EfEntityRepositoryBase<TenantSetting, HafizTakipDbContext>, ITenantSettingDal
{
    public EfTenantSettingDal(HafizTakipDbContext context) : base(context)
    {
    }
}
