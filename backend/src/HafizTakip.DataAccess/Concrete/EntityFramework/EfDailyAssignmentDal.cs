using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfDailyAssignmentDal : EfEntityRepositoryBase<DailyAssignment, HafizTakipDbContext>, IDailyAssignmentDal
{
    public EfDailyAssignmentDal(HafizTakipDbContext context) : base(context)
    {
    }
}
