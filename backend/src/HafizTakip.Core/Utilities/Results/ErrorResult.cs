namespace HafizTakip.Core.Utilities.Results;

public class ErrorResult : Result
{
    public ErrorResult(string message) : base(false, message) { }
    public ErrorResult() : base(false) { }

    public ErrorResult(string message, ResultErrorType errorType) : base(false, message)
    {
        ErrorType = errorType;
    }
}