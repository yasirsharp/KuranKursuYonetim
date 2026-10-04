using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfParentNotificationDal : EfEntityRepositoryBase<ParentNotification, HafizTakipDbContext>, IParentNotificationDal
{
    public EfParentNotificationDal(HafizTakipDbContext context) : base(context)
    {
    }
}
