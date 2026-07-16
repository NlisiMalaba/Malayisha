using Malayisha.Api.Contracts.Auth;
using Malayisha.Api.Contracts.Booking;
using Malayisha.Application.Common;
using Malayisha.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Malayisha.Api.Mapping;

internal static class BookingResultMapper
{
    public static IActionResult ToCreatedResult(Result<Guid> result) =>
        result.IsSuccess && result.Value != Guid.Empty
            ? new ObjectResult(new BookingCreatedResponse(result.Value))
            {
                StatusCode = StatusCodes.Status201Created
            }
            : ToErrorResult(result.ErrorCode);

    public static IActionResult ToActionResult(Result result) =>
        result.IsSuccess
            ? new NoContentResult()
            : ToErrorResult(result.ErrorCode);

    private static ObjectResult ToErrorResult(string? errorCode) =>
        new(new ErrorResponse(errorCode!))
        {
            StatusCode = BookingErrorMapper.ToStatusCode(errorCode)
        };
}
