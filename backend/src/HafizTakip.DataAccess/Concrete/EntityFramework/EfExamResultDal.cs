using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfExamResultDal : EfEntityRepositoryBase<ExamResult, HafizTakipDbContext>, IExamResultDal
{
    public EfExamResultDal(HafizTakipDbContext context) : base(context)
    {
    }
}
