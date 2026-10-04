using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfAnnouncementDal : EfEntityRepositoryBase<Announcement, HafizTakipDbContext>, IAnnouncementDal
{
    public EfAnnouncementDal(HafizTakipDbContext context) : base(context)
    {
    }
}
