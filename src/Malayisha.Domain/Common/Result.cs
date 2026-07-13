namespace Malayisha.Domain.Common;

public sealed class Result
{
    private Result(bool isSuccess, string? errorCode)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public string? ErrorCode { get; }

    public static Result Success() => new(true, null);

    public static Result Error(string errorCode) => new(false, errorCode);
}
