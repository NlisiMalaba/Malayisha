using Malayisha.Domain.Entities;

namespace Malayisha.Application.Abstractions.Persistence;

public interface IChatMessageRepository
{
    Task<IReadOnlyList<ChatMessage>> ListUndeliveredForRecipientAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessage>> ListByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
