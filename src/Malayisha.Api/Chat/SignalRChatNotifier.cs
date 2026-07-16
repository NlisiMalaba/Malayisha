using Malayisha.Application.Abstractions.Chat;
using Malayisha.Application.Features.Chat;
using Malayisha.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Malayisha.Api.Chat;

internal sealed class SignalRChatNotifier(IHubContext<ChatHub> hubContext) : IChatNotifier
{
    public Task NotifyMessageAsync(
        Guid bookingId,
        ChatMessageDto message,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients
            .Group(ChatHub.GetBookingGroupName(bookingId))
            .SendAsync(ChatConstants.ReceiveMessageMethod, message, cancellationToken);
}
