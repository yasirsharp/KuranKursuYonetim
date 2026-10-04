using Autofac;
using Autofac.Extensions.DependencyInjection;
using HafizTakip.Business.DependencyResolvers.Autofac;
using HafizTakip.Core.DependencyResolvers;
using HafizTakip.Core.Extensions;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Security.Encryption;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.WebAPI.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Autofac Configuration
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new AutofacBusinessModule());
    });

// Add services to the container.
builder.Services.AddControllers();

// Add CORS
// TODO: AllowAll politikasını dağıtım ortamında yapılandırılmış güvenilir origin listesiyle sınırla.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

// EF Core Configuration
builder.Services.AddDbContext<HafizTakipDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection"))
           .UseSnakeCaseNamingConvention(); // EfCore.NamingConventions eklentisi
});

// JWT Configuration
// TODO: Üretimde veritabanı bağlantısı ve JWT imza anahtarının secret store/environment üzerinden geldiğini zorunlu kıl; repodaki appsettings değerleri geliştirme içindir.
var tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = tokenOptions.Issuer,
            ValidAudience = tokenOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey)
        };
    });

// Core Dependencies
builder.Services.AddDependencyResolvers(new ICoreModule[]
{
    new CoreModule()
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// Authorization Policies
// TODO: Controller'lardaki çıplak [Authorize] yalnızca oturum kontrolü yapar; endpoint rolü ve veli-öğrenci kayıt sahipliği kurallarını tamamla.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSuperAdmin", policy => policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "super_admin"));
    options.AddPolicy("RequireTenantAdmin", policy => policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "super_admin", "tenant_admin"));
    options.AddPolicy("RequireHeadTeacher", policy => policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "super_admin", "tenant_admin", "head_teacher"));
    options.AddPolicy("RequireTeacher", policy => policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "super_admin", "tenant_admin", "head_teacher", "assistant_teacher"));
    options.AddPolicy("RequireParent", policy => policy.RequireClaim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "super_admin", "tenant_admin", "head_teacher", "assistant_teacher", "parent"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Custom Exception Middleware
app.ConfigureCustomExceptionMiddleware();

app.UseCors("AllowAll");

app.UseAuthentication();

// Tenant Resolution (Mutlaka Auth'dan sonra olmalı ki JWT Claims dolsun)
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthorization();

app.MapControllers();

// SADECE GELİŞTİRME ORTAMI İÇİN TEST VERİSİ ÜRETİCİSİ
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/dev/seed", async (HafizTakipDbContext db, HafizTakip.Core.Utilities.Security.Jwt.ITokenHelper tokenHelper) =>
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == "ornek");
        HafizTakip.Entities.Concrete.User? teacher = null;

        if (tenant == null)
        {
            tenant = new HafizTakip.Core.Entities.Platform.Tenant { Id = Guid.NewGuid(), Name = "Örnek Kurs", Slug = "ornek", IsActive = true, Status = "active" };
            db.Tenants.Add(tenant);
            
            teacher = new HafizTakip.Entities.Concrete.User { Id = Guid.NewGuid(), TenantId = tenant.Id, FirstName = "Ahmet", LastName = "Hoca", PhoneNumber = "5551112233", Role = "head_teacher" };
            db.Users.Add(teacher);
            
            var assistant = new HafizTakip.Entities.Concrete.User { Id = Guid.NewGuid(), TenantId = tenant.Id, FirstName = "Mehmet", LastName = "Hoca", PhoneNumber = "5551112244", Role = "assistant_teacher" };
            db.Users.Add(assistant);

            var group = new HafizTakip.Entities.Concrete.Group { Id = Guid.NewGuid(), TenantId = tenant.Id, TeacherId = teacher.Id, Name = "Hafızlık Sınıfı 1" };
            db.Groups.Add(group);
            
            var student1 = new HafizTakip.Entities.Concrete.Student { Id = Guid.NewGuid(), TenantId = tenant.Id, GroupId = group.Id, FirstName = "Yusuf", LastName = "Demir", IdentificationNumber = "123", CurrentPage = 156, CompletedPages = 156, Status = "active" };
            db.Students.Add(student1);
            
            var student2 = new HafizTakip.Entities.Concrete.Student { Id = Guid.NewGuid(), TenantId = tenant.Id, GroupId = group.Id, FirstName = "Zeynep", LastName = "Çelik", IdentificationNumber = "124", CurrentPage = 312, CompletedPages = 312, Status = "active" };
            db.Students.Add(student2);

            var lesson1 = new HafizTakip.Entities.Concrete.Lesson { Id = Guid.NewGuid(), TenantId = tenant.Id, StudentId = student1.Id, TeacherId = teacher.Id, LessonDate = DateTime.UtcNow.Date, JuzNumber = 7, PageNumber = 156, SuraId = 1, IsNewPage = true, HomeworkStatus = "yapıldı" };
            db.Lessons.Add(lesson1);

            await db.SaveChangesAsync();
        }
        else
        {
            teacher = await db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Role == "head_teacher");
            if (teacher == null) return Results.BadRequest("Öğretmen bulunamadı.");
        }

        // Admin token üret
        var platformUser = new HafizTakip.Core.Entities.Platform.PlatformUser { Id = teacher.Id, Email = "" };
        var token = tokenHelper.CreateToken(platformUser, new List<string> { teacher.Role }, tenant.Id);

        return Results.Ok(new { message = "Test verisi hazır.", token = token.Token, tenantId = tenant.Id, teacherId = teacher.Id });
    }).AllowAnonymous();
}

app.Run();
