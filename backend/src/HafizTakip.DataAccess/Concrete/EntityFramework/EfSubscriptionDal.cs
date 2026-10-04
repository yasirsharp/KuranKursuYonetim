using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.DataAccess.Abstract;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfSubscriptionDal : EfEntityRepositoryBase<Subscription, HafizTakipDbContext>, ISubscriptionDal
{
    public EfSubscriptionDal(HafizTakipDbContext context) : base(context)
    {
    }
}
