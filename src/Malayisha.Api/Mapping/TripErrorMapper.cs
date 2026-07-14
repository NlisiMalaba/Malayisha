using Malayisha.Application.Features.Trip;

namespace Malayisha.Api.Mapping;

internal static class TripErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            TripErrorCodes.ProfileNotFound => StatusCodes.Status404NotFound,
            TripErrorCodes.TripNotFound => StatusCodes.Status404NotFound,
            TripErrorCodes.NotTripOwner => StatusCodes.Status403Forbidden,
            TripErrorCodes.DepartureDateMustBeFuture => StatusCodes.Status400BadRequest,
            TripErrorCodes.ActiveBookingsBlockDelete => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
}
