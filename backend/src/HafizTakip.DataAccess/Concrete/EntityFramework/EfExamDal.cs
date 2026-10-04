using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfExamDal : EfEntityRepositoryBase<Exam, HafizTakipDbContext>, IExamDal
{
    public EfExamDal(HafizTakipDbContext context) : base(context)
    {
    }
}
