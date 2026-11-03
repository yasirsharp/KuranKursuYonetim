using HafizTakip.Core.DataAccess.EntityFramework;
using HafizTakip.Core.Enums;
using HafizTakip.DataAccess.Abstract;
using HafizTakip.Entities.Concrete;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class EfJoinRequestDal : EfEntityRepositoryBase<JoinRequest, HafizTakipDbContext>, IJoinRequestDal
{
    public EfJoinRequestDal(HafizTakipDbContext context) : base(context)
    {
    }

    public Task<bool> HasPendingAcrossTenantsAsync(string normalizedPhone) =>
        Context.JoinRequests
            .IgnoreQueryFilters()
            .AnyAsync(r => r.PhoneNumber == normalizedPhone && r.Status == JoinRequestStatus.Pending && !r.IsDeleted);
}
