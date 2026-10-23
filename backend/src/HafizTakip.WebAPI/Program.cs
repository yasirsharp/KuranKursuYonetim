using Microsoft.AspNetCore.HttpOverrides;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using HafizTakip.Business.DependencyResolvers.Autofac;
using HafizTakip.Core.DependencyResolvers;
using HafizTakip.Core.Extensions;
using HafizTakip.Core.Utilities.IoC;
using HafizTakip.Core.Utilities.Security.Encryption;
using HafizTakip.Core.Utilities.Security.Jwt;
using HafizTakip.Core.Utilities.Security.Otp;
using HafizTakip.Core.Utilities.Email;
using HafizTakip.Core.Utilities.Sms;
using HafizTakip.DataAccess.Concrete.EntityFramework;
using HafizTakip.WebAPI.Middlewares;
using HafizTakip.WebAPI.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Yapılandırma doğrulaması ─────────────────────────────────────────
// Secret'lar repoda tutulmaz: üretimde ortam değişkeni, geliştirmede appsettings.Development.json
// veya `dotnet user-secrets` kullanılır. Eksik/zayıf ayarla uygulama başlamaz.
var connectionString = builder.Configuration.GetConnectionString("PostgreSqlConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:PostgreSqlConnection yapılandırılmamış.");

var tokenOptions = builder.Configuration.GetSection("TokenOptions").Get<TokenOptions>() ?? new TokenOptions();
var tokenErrors = tokenOptions.Validate();
if (tokenErrors.Count > 0)
    throw new InvalidOperationException(string.Join(Environment.NewLine, tokenErrors));
if (!builder.Environment.IsDevelopment() && tokenOptions.SecurityKey.StartsWith("DEV-ONLY-"))
    throw new InvalidOperationException("Geliştirme JWT anahtarı geliştirme dışı bir ortamda kullanılamaz.");

// Test sunucusu (ASPNETCORE_ENVIRONMENT=Staging): gerçek SMS yok, sabit doğrulama kodu kullanılabilir.
// Canlı ortamda (Production) ikisi de yasaktır; uygulama başlamaz.
var isTestEnvironment = builder.Environment.IsDevelopment() || builder.Environment.IsStaging();

var otpOptions = builder.Configuration.GetSection("Otp").Get<OtpOptions>() ?? new OtpOptions();
if (!isTestEnvironment && !string.IsNullOrEmpty(otpOptions.FixedCode))
    throw new InvalidOperationException("Otp:FixedCode yalnızca geliştirme ve test (Staging) ortamında kullanılabilir.");

var smsSettings = builder.Configuration.GetSection("Sms").Get<SmsSettings>() ?? new SmsSettings();
if (!isTestEnvironment && smsSettings.Provider != SmsSettings.NetGsmProvider)
    throw new InvalidOperationException("Canlı ortamda Sms:Provider 'netgsm' olmalıdır.");

var emailSettings = builder.Configuration.GetSection("Email").Get<EmailSettings>() ?? new EmailSettings();
if (!isTestEnvironment && emailSettings.Provider != EmailSettings.SmtpProvider)
    throw new InvalidOperationException("Canlı ortamda Email:Provider 'smtp' olmalıdır.");

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Autofac Configuration
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule(new AutofacBusinessModule());
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Saatler "HH:mm" olarak gidip gelir
        options.JsonSerializerOptions.Converters.Add(new HafizTakip.Core.Utilities.Json.TimeOnlyJsonConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Gövde ayrıştırılamadığında da uygulamanın standart hata biçimi dönsün: { success, message, statusCode, errors }
        options.InvalidModelStateResponseFactory = context =>
        {
            var invalidEntries = context.ModelState.Where(entry => entry.Value?.Errors.Count > 0).ToList();

            // Gövdede alan bazlı JSON hatası varsa, ASP.NET'in eklediği genel "parametre zorunlu" hatasını gösterme
            if (invalidEntries.Any(entry => entry.Key.StartsWith('$')))
                invalidEntries = invalidEntries.Where(entry => entry.Key.StartsWith('$')).ToList();

            var errors = invalidEntries
                .SelectMany(entry => entry.Value!.Errors.Select(error => new ValidationErrorItem(
                    entry.Key.TrimStart('$', '.'),
                    string.IsNullOrEmpty(error.ErrorMessage) ? "Geçersiz değer." : error.ErrorMessage)))
                .ToList();

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new ValidationErrorDetails
            {
                StatusCode = StatusCodes.Status400BadRequest,
                Message = "Doğrulama hatası.",
                Errors = errors
            });
        };
    });

// CORS — yalnızca yapılandırmadaki origin'ler
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// EF Core Configuration
builder.Services.AddDbContext<HafizTakipDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention(); // EfCore.NamingConventions eklentisi
});

// JWT Configuration
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
            IssuerSigningKey = SecurityKeyHelper.CreateSecurityKey(tokenOptions.SecurityKey),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Kimlik doğrulama ayarları ve SMS sağlayıcısı
builder.Services.AddSingleton(tokenOptions);
builder.Services.AddSingleton(otpOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("Sms"));
if (smsSettings.Provider == SmsSettings.NetGsmProvider)
    builder.Services.AddHttpClient<ISmsSender, NetGsmSmsSender>(client => client.Timeout = TimeSpan.FromSeconds(15));
else
    builder.Services.AddSingleton<ISmsSender, ConsoleSmsSender>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
if (emailSettings.Provider == EmailSettings.SmtpProvider)
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

builder.Services.AddHafizTakipRateLimiting(builder.Configuration);

// Core Dependencies
builder.Services.AddDependencyResolvers(new ICoreModule[]
{
    new CoreModule()
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "verify-otp yanıtındaki token'ı girin (Bearer öneki otomatik eklenir).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearerScheme] = Array.Empty<string>() });
});

// Rol hiyerarşisine dayalı yetkilendirme policy'leri
builder.Services.AddHafizTakipAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Panelli hosting / Nginx gibi aynı makinedeki ters vekil arkasında gerçek istemci IP'si
// (hız sınırı IP başına çalışır). Varsayılan olarak yalnızca loopback vekile güvenilir.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Custom Exception Middleware
app.ConfigureCustomExceptionMiddleware();

// Tek alan adı düzeninde React uygulaması wwwroot'tadır: aynı alan adından sunulur (CORS gerekmez).
// Web ayrı alan adındaysa veya geliştirmede wwwroot yoktur; izinli origin'ler Cors:AllowedOrigins'tedir.
var serveSpa = File.Exists(Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "index.html"));
if (serveSpa)
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseAuthentication();

// Tenant Resolution (Mutlaka Auth'dan sonra olmalı ki JWT Claims dolsun)
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseAuthorization();

app.MapControllers();

if (serveSpa)
{
    // Bilinmeyen API adresi 404 dönsün; diğer her adres React yönlendiricisine (index.html) gider
    app.MapFallback("/api/{**rest}", () => Results.NotFound());
    app.MapFallbackToFile("index.html");
}
else
{
    // Web ayrı alan adındayken API'nin kök adresi çalıştığını gösterir (yayından sonra kontrol için)
    app.MapGet("/", () => Results.Ok(new { service = "Hafız Takip API", status = "çalışıyor" }));
}

// Şema EF tarafından oluşturulmaz (SQL script-first): veritabanını database/db.ps1 -Setup ile kurun.
// SADECE GELİŞTİRME ORTAMI İÇİN — test verisi üreten uç
if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Dev:EnableSeed"))
{
    app.MapPost("/api/dev/seed", async (HafizTakipDbContext db, HafizTakip.Core.Utilities.Security.Jwt.ITokenHelper tokenHelper) =>
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == "ornek");
        HafizTakip.Entities.Concrete.User? teacher = null;

        if (tenant == null)
        {
            // 1. Önce Tenant oluştur ve kaydet
            tenant = new HafizTakip.Core.Entities.Platform.Tenant { Id = Guid.NewGuid(), Name = "Örnek Kurs", Slug = "ornek", IsActive = true, Status = "active" };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            // 2. User'ları oluştur ve kaydet (Tenant FK bağımlılığı)
            teacher = new HafizTakip.Entities.Concrete.User { Id = Guid.NewGuid(), TenantId = tenant.Id, FirstName = "Ahmet", LastName = "Hoca", PhoneNumber = "5551112233", Role = "head_teacher" };
            db.Users.Add(teacher);

            var assistant = new HafizTakip.Entities.Concrete.User { Id = Guid.NewGuid(), TenantId = tenant.Id, FirstName = "Mehmet", LastName = "Hoca", PhoneNumber = "5551112244", Role = "assistant_teacher" };
            db.Users.Add(assistant);
            await db.SaveChangesAsync();

            // 3. Group oluştur ve kaydet (Tenant + User FK bağımlılığı)
            var group = new HafizTakip.Entities.Concrete.Group { Id = Guid.NewGuid(), TenantId = tenant.Id, TeacherId = teacher.Id, Name = "Hafızlık Sınıfı 1" };
            db.Groups.Add(group);
            await db.SaveChangesAsync();

            // 4. Student'ları oluştur ve kaydet (Tenant + Group FK bağımlılığı)
            var student1 = new HafizTakip.Entities.Concrete.Student { Id = Guid.NewGuid(), TenantId = tenant.Id, GroupId = group.Id, FirstName = "Yusuf", LastName = "Demir", IdentificationNumber = "123", CurrentPage = 156, CompletedPages = 156, Status = "active" };
            db.Students.Add(student1);

            var student2 = new HafizTakip.Entities.Concrete.Student { Id = Guid.NewGuid(), TenantId = tenant.Id, GroupId = group.Id, FirstName = "Zeynep", LastName = "Çelik", IdentificationNumber = "124", CurrentPage = 312, CompletedPages = 312, Status = "active" };
            db.Students.Add(student2);
            await db.SaveChangesAsync();

            // 5. Lesson oluştur ve kaydet (Student + Teacher FK bağımlılığı)
            var lesson1 = new HafizTakip.Entities.Concrete.Lesson { Id = Guid.NewGuid(), TenantId = tenant.Id, StudentId = student1.Id, TeacherId = teacher.Id, LessonDate = DateTime.UtcNow.Date, JuzNumber = 8, PageNumber = 156, SuraId = 7, IsNewPage = true, HomeworkStatus = "yapildi" };
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

// WebApplicationFactory tabanlı entegrasyon testleri için
public partial class Program { }
