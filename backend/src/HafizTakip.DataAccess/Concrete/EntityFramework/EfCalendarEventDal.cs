using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfCalendarEventDal : EfEntityRepositoryBase<CalendarEvent, HafizTakipDbContext>, ICalendarEventDal
{
    public EfCalendarEventDal(HafizTakipDbContext context) : base(context)
    {
    }
}
