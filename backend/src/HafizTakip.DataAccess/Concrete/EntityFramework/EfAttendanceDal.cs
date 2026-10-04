using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfAttendanceDal : EfEntityRepositoryBase<Attendance, HafizTakipDbContext>, IAttendanceDal
{
    public EfAttendanceDal(HafizTakipDbContext context) : base(context)
    {
    }
}
