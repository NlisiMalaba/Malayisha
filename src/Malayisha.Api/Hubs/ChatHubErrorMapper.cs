using Malayisha.Application.Features.Chat;
using Microsoft.AspNetCore.SignalR;

namespace Malayisha.Api.Hubs;

internal static class ChatHubErrorMapper
{
    public static HubException ToHubException(string errorCode) =>
        errorCode switch
        {
            ChatErrorCodes.MessageTooLong => new HubException(ChatErrorCodes.MessageTooLong),
            ChatErrorCodes.NotBookingParticipant => new HubException(ChatErrorCodes.NotBookingParticipant),
            ChatErrorCodes.BookingNotFound => new HubException(ChatErrorCodes.BookingNotFound),
            _ => new HubException(errorCode)
        };
}
