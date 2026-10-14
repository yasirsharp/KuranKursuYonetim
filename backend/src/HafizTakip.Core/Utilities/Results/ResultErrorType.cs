namespace HafizTakip.Core.Utilities.Results;

/// <summary>
/// Başarısız bir sonucun türü. Controller katmanı bunu HTTP durum koduna çevirir.
/// </summary>
public enum ResultErrorType
{
    None = 0,
    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    TooManyRequests = 429
}