using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfRepetitionSessionDal : EfEntityRepositoryBase<RepetitionSession, HafizTakipDbContext>, IRepetitionSessionDal
{
    public EfRepetitionSessionDal(HafizTakipDbContext context) : base(context)
    {
    }
}
