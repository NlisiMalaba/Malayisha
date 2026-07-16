using Malayisha.Application.Features.Chat;

namespace Malayisha.Api.Mapping;

internal static class ChatErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            ChatErrorCodes.BookingNotFound => StatusCodes.Status404NotFound,
            ChatErrorCodes.NotBookingParticipant => StatusCodes.Status403Forbidden,
            ChatErrorCodes.MessageTooLong => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
}
