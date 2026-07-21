using Malayisha.Application.Features.Booking;

namespace Malayisha.Api.Mapping;

internal static class BookingErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            BookingErrorCodes.BookingNotFound => StatusCodes.Status404NotFound,
            BookingErrorCodes.NotBookingParticipant => StatusCodes.Status403Forbidden,
            BookingErrorCodes.TripNotFound => StatusCodes.Status404NotFound,
            BookingErrorCodes.TransporterProfileNotFound => StatusCodes.Status404NotFound,
            BookingErrorCodes.DeliveryRequestNotFound => StatusCodes.Status404NotFound,
            BookingErrorCodes.DeliveryRequestNotActive => StatusCodes.Status409Conflict,
            BookingErrorCodes.DeliveryRequestNotOwnedBySender => StatusCodes.Status403Forbidden,
            BookingErrorCodes.DeliveryRequestAlreadyBooked => StatusCodes.Status409Conflict,
            BookingErrorCodes.SelfBookingNotAllowed => StatusCodes.Status409Conflict,
            "InvalidStateTransition" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };
}
