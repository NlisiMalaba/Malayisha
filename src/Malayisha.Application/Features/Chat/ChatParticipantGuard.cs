namespace Malayisha.Application.Features.Chat;

internal static class ChatParticipantGuard
{
    public static bool IsParticipant(Domain.Entities.Booking booking, Guid userId) =>
        booking.SenderId == userId || booking.TransporterId == userId;

    public static Guid GetOtherParticipantId(Domain.Entities.Booking booking, Guid userId) =>
        booking.SenderId == userId ? booking.TransporterId : booking.SenderId;
}
