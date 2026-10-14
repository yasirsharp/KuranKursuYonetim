using System.Security.Authentication;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HafizTakip.Core.Extensions;

/// <summary>
/// Yakalanmamış tüm hataları standart { success, message, statusCode } JSON yanıtına çevirir.
/// Beklenmeyen hataların ayrıntısı yalnızca loglanır; üretimde istemciye sızdırılmaz.
/// </summary>
public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception e)
        {
            await HandleExceptionAsync(httpContext, e);
        }
    }

    private Task HandleExceptionAsync(HttpContext httpContext, Exception e)
    {
        ErrorDetails details;

        switch (e)
        {
            case FluentValidation.ValidationException validationException:
                details = new ValidationErrorDetails
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Doğrulama hatası.",
                    Errors = validationException.Errors
                        .Select(f => new ValidationErrorItem(f.PropertyName, f.ErrorMessage))
                        .ToList()
                };
                break;

            case AuthenticationException:
                details = new ErrorDetails
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = "Kimlik doğrulama başarısız."
                };
                break;

            case UnauthorizedAccessException:
                details = new ErrorDetails
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bu işlem için yetkiniz yok."
                };
                break;

            // Veritabanı kısıtları: uygulama kontrolünü aşan eşzamanlı isteklerde son savunma hattı
            case DbUpdateException dbException when GetSqlState(dbException) == UniqueViolation:
                details = new ErrorDetails
                {
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Bu kayıt zaten mevcut."
                };
                break;

            case DbUpdateException dbException when GetSqlState(dbException) == ForeignKeyViolation:
                details = new ErrorDetails
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "İlişkili kayıt bulunamadı veya bu kursa ait değil."
                };
                break;

            default:
                _logger.LogError(e, "İşlenmeyen hata: {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                details = new ErrorDetails
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = _environment.IsDevelopment()
                        ? $"Beklenmeyen bir hata oluştu. Detay: {e.Message}"
                        : "Beklenmeyen bir hata oluştu."
                };
                break;
        }

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = details.StatusCode;
        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(details, details.GetType(), JsonOptions));
    }

    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";

    /// <summary>
    /// İç hatalardaki PostgreSQL SQLSTATE kodunu okur. Core katmanı Npgsql'e bağımlı olmasın diye
    /// PostgresException.SqlState yansıma (reflection) ile alınır.
    /// </summary>
    private static string? GetSqlState(Exception exception)
    {
        for (var inner = exception.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner.GetType().GetProperty("SqlState")?.GetValue(inner) is string sqlState)
                return sqlState;
        }
        return null;
    }
}

public class ErrorDetails
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
}

public class ValidationErrorDetails : ErrorDetails
{
    public List<ValidationErrorItem> Errors { get; set; } = new();
}

public record ValidationErrorItem(string PropertyName, string ErrorMessage);
