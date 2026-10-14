using HafizTakip.Core.Entities;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.Entities.Concrete;
using HafizTakip.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HafizTakip.IntegrationTests.Data;

/// <summary>EF modeli üzerinde, veritabanına bağlanmadan çalışan kontroller.</summary>
public class ModelConfigurationTests
{
    private static HafizTakipDbContext CreateOfflineContext()
    {
        var options = new DbContextOptionsBuilder<HafizTakipDbContext>()
            .UseNpgsql("Host=offline;Database=model_only")
            .UseSnakeCaseNamingConvention()
            .Options;
        return new HafizTakipDbContext(options, new TestTenantContext(Guid.NewGuid()));
    }

    [Fact]
    public void EveryTenantEntity_ShouldHaveTenantAndSoftDeleteQueryFilter()
    {
        using var context = CreateOfflineContext();

        var tenantEntityTypes = context.Model.GetEntityTypes()
            .Where(t => typeof(ITenantEntity).IsAssignableFrom(t.ClrType))
            .ToList();

        tenantEntityTypes.Should().NotBeEmpty();
        foreach (var entityType in tenantEntityTypes)
        {
            var filter = entityType.GetQueryFilter();
            filter.Should().NotBeNull($"{entityType.ClrType.Name} tenant filtresi olmadan sorgulanamaz");
            filter!.ToString().Should().Contain("TenantId").And.Contain("IsDeleted");
        }
    }

    [Fact]
    public void EveryPlatformEntity_ShouldHaveSoftDeleteQueryFilter()
    {
        using var context = CreateOfflineContext();

        var platformTypes = context.Model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType) && !typeof(ITenantEntity).IsAssignableFrom(t.ClrType))
            .ToList();

        platformTypes.Should().NotBeEmpty();
        foreach (var entityType in platformTypes)
        {
            entityType.GetQueryFilter()!.ToString().Should().Contain("IsDeleted");
        }
    }

    [Fact]
    public void DailyAssignmentTaskLists_ShouldMapToJsonb()
    {
        using var context = CreateOfflineContext();
        var entity = context.Model.FindEntityType(typeof(DailyAssignment))!;

        foreach (var property in new[] { nameof(DailyAssignment.AssignedTasks), nameof(DailyAssignment.CompletedInEtut), nameof(DailyAssignment.CompletedAtHome) })
        {
            entity.FindProperty(property)!.GetColumnType().Should().Be("jsonb");
        }
    }
}
