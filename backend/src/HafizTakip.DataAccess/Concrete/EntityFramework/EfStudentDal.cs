using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfStudentDal : EfEntityRepositoryBase<Student, HafizTakipDbContext>, IStudentDal
{
    public EfStudentDal(HafizTakipDbContext context) : base(context)
    {
    }
}
