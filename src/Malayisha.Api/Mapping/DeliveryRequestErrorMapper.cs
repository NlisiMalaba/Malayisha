using Malayisha.Application.Features.DeliveryRequest;

namespace Malayisha.Api.Mapping;

internal static class DeliveryRequestErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            DeliveryRequestErrorCodes.DeliveryRequestNotFound => StatusCodes.Status404NotFound,
            DeliveryRequestErrorCodes.NotDeliveryRequestOwner => StatusCodes.Status403Forbidden,
            DeliveryRequestErrorCodes.RequiredDateMustBeFuture => StatusCodes.Status400BadRequest,
            DeliveryRequestErrorCodes.AssociatedBookingBlocksUpdate => StatusCodes.Status409Conflict,
            DeliveryRequestErrorCodes.ActiveBookingsBlockCancel => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
}
