using HafizTakip.Core.Utilities.Results;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Entities.Concrete;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using static HafizTakip.IntegrationTests.Infrastructure.PostgresDatabaseFixture;

namespace HafizTakip.IntegrationTests.Data;

[Collection(PostgresCollection.Name)]
public class UnitOfWorkTests
{
    private readonly PostgresDatabaseFixture _db;

    public UnitOfWorkTests(PostgresDatabaseFixture db) => _db = db;

    private async Task<bool> GroupExistsAsync(string name)
    {
        await using var context = _db.CreateContext(TenantA);
        return await context.Groups.AnyAsync(g => g.Name == name);
    }

    [PostgresFact]
    public async Task SuccessfulWork_ShouldCommitAllChangesTogether()
    {
        var name = $"Grup-{Guid.NewGuid():N}";
        await using var context = _db.CreateContext(TenantA);
        var unitOfWork = new EfUnitOfWork(context);
        var groups = new EfGroupDal(context);

        var result = await unitOfWork.ExecuteInTransactionAsync<IResult>(async () =>
        {
            groups.Add(new Group { TeacherId = TeacherA, Name = name });
            groups.Add(new Group { TeacherId = TeacherA, Name = name + "-2" });
            await Task.CompletedTask;
            return new SuccessResult();
        });

        result.Success.Should().BeTrue();
        (await GroupExistsAsync(name)).Should().BeTrue();
        (await GroupExistsAsync(name + "-2")).Should().BeTrue();
    }

    [PostgresFact]
    public async Task FailedResult_ShouldRollBackEvenImmediatelySavedChanges()
    {
        var name = $"Grup-{Guid.NewGuid():N}";
        await using var context = _db.CreateContext(TenantA);
        var unitOfWork = new EfUnitOfWork(context);
        var groups = new EfGroupDal(context);

        var result = await unitOfWork.ExecuteInTransactionAsync<IResult>(async () =>
        {
            await groups.AddAsync(new Group { TeacherId = TeacherA, Name = name }); // hemen kaydeden eski metot
            return new ErrorResult("iş kuralı ihlali");
        });

        result.Success.Should().BeFalse();
        (await GroupExistsAsync(name)).Should().BeFalse("başarısız sonuçta transaction geri alınmalı");
    }

    [PostgresFact]
    public async Task Exception_ShouldRollBackAndRethrow()
    {
        var name = $"Grup-{Guid.NewGuid():N}";
        await using var context = _db.CreateContext(TenantA);
        var unitOfWork = new EfUnitOfWork(context);
        var groups = new EfGroupDal(context);

        var act = () => unitOfWork.ExecuteInTransactionAsync<IResult>(async () =>
        {
            await groups.AddAsync(new Group { TeacherId = TeacherA, Name = name });
            throw new InvalidOperationException("beklenmeyen hata");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await GroupExistsAsync(name)).Should().BeFalse();
    }

    [PostgresFact]
    public async Task DatabaseConstraintViolation_ShouldRollBackEarlierWrites()
    {
        var name = $"Grup-{Guid.NewGuid():N}";
        await using var context = _db.CreateContext(TenantA);
        var unitOfWork = new EfUnitOfWork(context);
        var groups = new EfGroupDal(context);

        // İkinci grup başka kursun hocasına bağlanıyor: bileşik FK reddeder → ilk grup da kaydedilmemeli
        var act = () => unitOfWork.ExecuteInTransactionAsync<IResult>(async () =>
        {
            await groups.AddAsync(new Group { TeacherId = TeacherA, Name = name });
            groups.Add(new Group { TeacherId = TeacherB, Name = name + "-yabanci" });
            await Task.CompletedTask;
            return new SuccessResult();
        });

        await act.Should().ThrowAsync<DbUpdateException>();
        (await GroupExistsAsync(name)).Should().BeFalse();
    }
}
