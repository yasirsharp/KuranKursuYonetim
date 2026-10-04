using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfEtutNoteDal : EfEntityRepositoryBase<EtutNote, HafizTakipDbContext>, IEtutNoteDal
{
    public EfEtutNoteDal(HafizTakipDbContext context) : base(context)
    {
    }
}
