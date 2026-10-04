using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfLessonDal : EfEntityRepositoryBase<Lesson, HafizTakipDbContext>, ILessonDal
{
    public EfLessonDal(HafizTakipDbContext context) : base(context)
    {
    }
}
