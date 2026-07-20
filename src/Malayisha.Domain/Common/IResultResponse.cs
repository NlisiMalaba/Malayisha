namespace Malayisha.Domain.Common;

public interface IResultResponse
{
    bool IsSuccess { get; }

    string? ErrorCode { get; }
}
