using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfGroupDal : EfEntityRepositoryBase<Group, HafizTakipDbContext>, IGroupDal
{
    public EfGroupDal(HafizTakipDbContext context) : base(context)
    {
    }
}
