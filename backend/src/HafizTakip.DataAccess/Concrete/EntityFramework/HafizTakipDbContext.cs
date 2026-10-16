using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using HafizTakip.Core.Entities;
using HafizTakip.Core.Entities.Platform;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Entities.Concrete;

namespace HafizTakip.DataAccess.Concrete.EntityFramework;

public class HafizTakipDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public HafizTakipDbContext(DbContextOptions<HafizTakipDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Platform Tabloları
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<PlatformUser> PlatformUsers { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    // Tenant Tabloları
    public DbSet<User> Users { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Attendance> Attendances { get; set; }
    public DbSet<RepetitionSession> RepetitionSessions { get; set; }
    public DbSet<EtutNote> EtutNotes { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<ParentNotification> ParentNotifications { get; set; }
    public DbSet<DailyAssignment> DailyAssignments { get; set; }
    public DbSet<StudentParent> StudentParents { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<TenantSetting> TenantSettings { get; set; }

    // Kur'an referans verisi (platform seviyesi, salt okunur)
    public DbSet<QuranSurah> QuranSurahs { get; set; }
    public DbSet<QuranPage> QuranPages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tablo/kolon adları Program.cs'teki UseSnakeCaseNamingConvention ile eşlenir.

        // Global Query Filters — her entity için otomatik:
        // ITenantEntity → silinmemiş VE yalnızca mevcut kursa ait kayıtlar
        // Diğer BaseEntity'ler (platform tabloları) → yalnızca silinmemiş kayıtlar
        // Yeni bir entity DbSet olarak eklendiğinde filtre kendiliğinden uygulanır.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;
            if (!typeof(BaseEntity).IsAssignableFrom(clrType)) continue;

            var method = typeof(ITenantEntity).IsAssignableFrom(clrType)
                ? ApplyTenantFilterMethod
                : ApplySoftDeleteFilterMethod;
            method.MakeGenericMethod(clrType).Invoke(this, new object[] { modelBuilder });
        }

        modelBuilder.Entity<QuranSurah>().Property(s => s.Id).ValueGeneratedNever();
        modelBuilder.Entity<QuranPage>(b =>
        {
            b.HasKey(p => p.Page);
            b.Property(p => p.Page).ValueGeneratedNever();
        });

        // DailyAssignment görev listeleri veritabanında JSONB olarak tutulur
        modelBuilder.Entity<DailyAssignment>(b =>
        {
            b.Property(d => d.AssignedTasks).HasColumnType("jsonb");
            b.Property(d => d.CompletedInEtut).HasColumnType("jsonb");
            b.Property(d => d.CompletedAtHome).HasColumnType("jsonb");
        });

        ConfigureDateTimes(modelBuilder);
    }

    // ── Tarih / zaman eşlemesi ──────────────────────────────────────

    /// <summary>timestamptz: her zaman UTC yazılır ve UTC okunur (Kind'ı belirsiz değerler UTC kabul edilir).</summary>
    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        v => v.Kind == DateTimeKind.Utc ? v
            : v.Kind == DateTimeKind.Local ? v.ToUniversalTime()
            : DateTime.SpecifyKind(v, DateTimeKind.Utc),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    /// <summary>date: yalnızca gün yazılır; saat ve saat dilimi bilgisi taşımaz.</summary>
    private static readonly ValueConverter<DateTime, DateTime> CalendarDateConverter = new(
        v => DateTime.SpecifyKind(v.Date, DateTimeKind.Unspecified),
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified));

    /// <summary>
    /// Şemada gün bilgisi tutan kolonlar DATE (adı "Date" olan veya "...Date" ile biten özellikler),
    /// anlık zamanlar TIMESTAMPTZ'dir. Npgsql'in eski zaman davranışı kullanılmaz: o modda UTC değerler
    /// "saat dilimsiz" gönderilip sunucu saat dilimine göre kaydırılıyordu (Türkiye'de 3 saat geri).
    /// Yeni bir DateTime özelliği bu kurala göre kendiliğinden eşlenir.
    /// </summary>
    private static void ConfigureDateTimes(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties()
                         .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
            {
                if (IsCalendarDate(property.Name))
                {
                    property.SetColumnType("date");
                    property.SetValueConverter(CalendarDateConverter);
                }
                else
                {
                    property.SetColumnType("timestamp with time zone");
                    property.SetValueConverter(UtcConverter);
                }
            }
        }
    }

    public static bool IsCalendarDate(string propertyName) =>
        propertyName == "Date" || propertyName.EndsWith("Date", StringComparison.Ordinal);

    private static readonly MethodInfo ApplyTenantFilterMethod =
        typeof(HafizTakipDbContext).GetMethod(nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ApplySoftDeleteFilterMethod =
        typeof(HafizTakipDbContext).GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity, ITenantEntity
    {
        // _tenantContext bu DbContext örneğine bağlıdır; EF her sorguda güncel TenantId'yi parametre olarak kullanır
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
    }

    private void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : BaseEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => !e.IsDeleted);
    }

    // ── Kaydetme güvenceleri ────────────────────────────────────────

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditAndTenantRules();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditAndTenantRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// - Yeni kurs kayıtlarına mevcut TenantId'yi basar; başka bir kursa yazılmasını engeller.
    /// - Mevcut kaydın TenantId'sinin değiştirilmesini engeller.
    /// - UpdatedAt'i günceller; fiziksel silmeyi soft delete'e çevirir.
    /// Tenant bağlamı boşken (giriş, token yenileme, dev seed, arka plan işi) bağlam kontrolü atlanır;
    /// bu akışlar TenantId'yi açıkça verir.
    /// </summary>
    private void ApplyAuditAndTenantRules()
    {
        var now = DateTime.UtcNow;
        var currentTenantId = _tenantContext.TenantId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
        {
            if (entry.Entity is ITenantEntity tenantEntity)
            {
                EnforceTenant(entry, tenantEntity, currentTenantId);
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;

                case EntityState.Deleted:
                    // Fiziksel silme yapılmaz (KVKK silme talepleri ayrı bir akışla ele alınacak)
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt ??= now;
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }

    private static void EnforceTenant(EntityEntry<BaseEntity> entry, ITenantEntity entity, Guid currentTenantId)
    {
        var entityName = entry.Metadata.ClrType.Name;

        switch (entry.State)
        {
            case EntityState.Added:
                if (entity.TenantId == Guid.Empty)
                {
                    if (currentTenantId == Guid.Empty)
                        throw new InvalidOperationException($"{entityName} kaydı için kurs (tenant) belirlenemedi.");
                    entity.TenantId = currentTenantId;
                }
                else if (currentTenantId != Guid.Empty && entity.TenantId != currentTenantId)
                {
                    throw new InvalidOperationException($"{entityName} kaydı başka bir kursa yazılamaz.");
                }
                break;

            case EntityState.Modified:
            case EntityState.Deleted:
                var tenantProperty = entry.Property(nameof(ITenantEntity.TenantId));
                if (tenantProperty.IsModified && !Equals(tenantProperty.OriginalValue, tenantProperty.CurrentValue))
                    throw new InvalidOperationException($"{entityName} kaydının kursu (TenantId) değiştirilemez.");

                if (currentTenantId != Guid.Empty && entity.TenantId != currentTenantId)
                    throw new InvalidOperationException($"{entityName} kaydı başka bir kursa ait; değiştirilemez.");
                break;
        }
    }
}
