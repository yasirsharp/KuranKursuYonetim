using Microsoft.EntityFrameworkCore;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tablo İsimleri (snake_case kurallarına göre mapping yapılabilir, 
        // ancak EF Core Npgsql naming convention plugin'i de kullanılabilir.
        // Şimdilik Npgsql.EntityFrameworkCore.PostgreSQL paketinin UseSnakeCaseNamingConvention metodunu
        // Program.cs'te kullanacağımız için burada elle tablo adı belirtmiyoruz.)

        // Global Query Filters
        // Tüm sorgularda varsayılan olarak silinmemiş olanları getir
        // Ve ITenantEntity ise, sadece ilgili tenant_id'ye ait olanları getir
        
        // Örnek: User filtresi
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Group>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Student>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Lesson>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Attendance>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<RepetitionSession>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<EtutNote>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Exam>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ExamResult>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<Announcement>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<ParentNotification>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<DailyAssignment>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        modelBuilder.Entity<StudentParent>().HasQueryFilter(e => !e.IsDeleted && e.TenantId == _tenantContext.TenantId);
        
        // Platform entity'leri sadece soft-delete filtresine takılır
        modelBuilder.Entity<Tenant>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PlatformUser>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Subscription>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Invoice>().HasQueryFilter(e => !e.IsDeleted);
    }
}
