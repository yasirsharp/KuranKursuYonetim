using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.DataAccess.Abstract;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfInvoiceDal : EfEntityRepositoryBase<Invoice, HafizTakipDbContext>, IInvoiceDal
{
    public EfInvoiceDal(HafizTakipDbContext context) : base(context)
    {
    }
}
