using Malayisha.Application.Features.Chat;

namespace Malayisha.Application.Abstractions.Chat;

public interface IChatNotifier
{
    Task NotifyMessageAsync(
        Guid bookingId,
        ChatMessageDto message,
        CancellationToken cancellationToken = default);
}
