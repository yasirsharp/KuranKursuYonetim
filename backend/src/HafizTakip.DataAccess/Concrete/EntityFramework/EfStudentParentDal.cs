using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfStudentParentDal : EfEntityRepositoryBase<StudentParent, HafizTakipDbContext>, IStudentParentDal
{
    public EfStudentParentDal(HafizTakipDbContext context) : base(context)
    {
    }
}
