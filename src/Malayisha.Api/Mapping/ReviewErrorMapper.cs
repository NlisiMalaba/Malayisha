using Malayisha.Application.Features.Review;

namespace Malayisha.Api.Mapping;

internal static class ReviewErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            ReviewErrorCodes.BookingNotFound => StatusCodes.Status404NotFound,
            ReviewErrorCodes.TransporterProfileNotFound => StatusCodes.Status404NotFound,
            ReviewErrorCodes.TripNotFound => StatusCodes.Status404NotFound,
            ReviewErrorCodes.NotBookingSender => StatusCodes.Status403Forbidden,
            ReviewErrorCodes.ReviewAlreadyExists => StatusCodes.Status409Conflict,
            ReviewErrorCodes.BookingNotCompleted => StatusCodes.Status422UnprocessableEntity,
            ReviewErrorCodes.InvalidRating => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
}
