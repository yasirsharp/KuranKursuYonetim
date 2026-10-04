using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfUserDal : EfEntityRepositoryBase<User, HafizTakipDbContext>, IUserDal
{
    public EfUserDal(HafizTakipDbContext context) : base(context)
    {
    }
}
