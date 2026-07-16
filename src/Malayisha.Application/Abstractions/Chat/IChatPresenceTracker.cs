namespace Malayisha.Application.Abstractions.Chat;

public interface IChatPresenceTracker
{
    Task ConnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);

    Task DisconnectAsync(Guid userId, string connectionId, CancellationToken cancellationToken = default);

    Task<bool> IsUserConnectedAsync(Guid userId, CancellationToken cancellationToken = default);
}
