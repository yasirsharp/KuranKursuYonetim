using System.ComponentModel.DataAnnotations;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HafizTakip.Core.Extensions;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
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
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.InternalServerError;

        string message = "Internal Server Error";
        IEnumerable<ValidationFailure>? errors = null;

        if (e.GetType() == typeof(FluentValidation.ValidationException))
        {
            var validationException = (FluentValidation.ValidationException)e;
            message = "Doğrulama hatası.";
            errors = validationException.Errors;
            httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.BadRequest;
            
            return httpContext.Response.WriteAsync(new ValidationErrorDetails
            {
                StatusCode = httpContext.Response.StatusCode,
                Message = message,
                Errors = errors
            }.ToString());
        }

        if (e.GetType() == typeof(UnauthorizedAccessException))
        {
            message = "Yetkisiz erişim.";
            httpContext.Response.StatusCode = (int)System.Net.HttpStatusCode.Unauthorized;
        }

        // TODO: Üretim yanıtında e.Message döndürme; ayrıntıları yalnızca sunucu tarafındaki güvenli loglara yaz.
        return httpContext.Response.WriteAsync(new ErrorDetails
        {
            StatusCode = httpContext.Response.StatusCode,
            Message = message + " Detay: " + e.Message
        }.ToString());
    }
}

public class ErrorDetails
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }

    public override string ToString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this);
    }
}

public class ValidationErrorDetails : ErrorDetails
{
    public IEnumerable<ValidationFailure>? Errors { get; set; }
}
