using Malayisha.Application.Abstractions.Persistence;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Malayisha.Infrastructure.Persistence.Repositories;

internal sealed class ChatMessageRepository(MalayishaDbContext dbContext) : IChatMessageRepository
{
    public async Task<IReadOnlyList<ChatMessage>> ListUndeliveredForRecipientAsync(
        Guid recipientUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ChatMessages
            .Where(message =>
                !message.IsDelivered
                && message.SenderUserId != recipientUserId
                && dbContext.Bookings.Any(booking =>
                    booking.Id == message.BookingId
                    && (booking.SenderId == recipientUserId || booking.TransporterId == recipientUserId)
                    && booking.Status != BookingStatus.Completed
                    && booking.Status != BookingStatus.Cancelled))
            .OrderBy(message => message.SentAtUtc)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await dbContext.ChatMessages.AddAsync(message, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
