using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfAcademicTermDal : EfEntityRepositoryBase<AcademicTerm, HafizTakipDbContext>, IAcademicTermDal
{
    public EfAcademicTermDal(HafizTakipDbContext context) : base(context)
    {
    }
}
