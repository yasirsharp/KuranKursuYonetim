using HafizTakip.Core.Utilities.Results;
using Microsoft.AspNetCore.Mvc;
using IResult = HafizTakip.Core.Utilities.Results.IResult;

namespace HafizTakip.WebAPI.Controllers;

/// <summary>
/// Business katmanından dönen IResult'ı uygun HTTP durum koduna çeviren ortak taban sınıf.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult(IResult result)
    {
        if (result.Success)
            return Ok(result);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(result),
            ResultErrorType.Conflict => Conflict(result),
            ResultErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, result),
            ResultErrorType.Unauthorized => Unauthorized(result),
            ResultErrorType.TooManyRequests => StatusCode(StatusCodes.Status429TooManyRequests, result),
            _ => BadRequest(result)
        };
    }
}
