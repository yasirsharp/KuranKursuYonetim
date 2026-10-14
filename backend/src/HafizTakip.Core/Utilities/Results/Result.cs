using System.Text.Json.Serialization;

namespace HafizTakip.Core.Utilities.Results;

public class Result : IResult
{
    public Result(bool success, string message) : this(success)
    {
        Message = message;
    }

    public Result(bool success)
    {
        Success = success;
        ErrorType = success ? ResultErrorType.None : ResultErrorType.BadRequest;
    }

    public bool Success { get; }
    public string Message { get; } = string.Empty;

    [JsonIgnore]
    public ResultErrorType ErrorType { get; protected init; }
}