namespace Malayisha.Domain.Common;

public sealed class Result : IResultResponse
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

    public static Result Invalid(string errorCode) => Error(errorCode);

    public static Result Forbidden() => Error("Forbidden");

    public static Result Unauthorized() => Error("Unauthorized");
}
