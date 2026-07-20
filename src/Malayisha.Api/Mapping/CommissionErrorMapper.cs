using Malayisha.Application.Features.Commission;

namespace Malayisha.Api.Mapping;

internal static class CommissionErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            CommissionErrorCodes.CommissionRecordNotFound => StatusCodes.Status404NotFound,
            CommissionErrorCodes.InvalidCommissionStatus => StatusCodes.Status422UnprocessableEntity,
            CommissionErrorCodes.InvalidDateRange => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
}
