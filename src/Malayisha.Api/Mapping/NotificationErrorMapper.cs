using Malayisha.Application.Features.Notifications;

namespace Malayisha.Api.Mapping;

internal static class NotificationErrorMapper
{
    public static int ToStatusCode(string? errorCode) =>
        errorCode switch
        {
            NotificationErrorCodes.UserNotFound => StatusCodes.Status404NotFound,
            NotificationErrorCodes.UserInactive => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };
}
