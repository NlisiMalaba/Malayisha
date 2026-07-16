namespace Malayisha.Application.Features.Chat;

public sealed record ChatMessageDto(
    Guid Id,
    Guid BookingId,
    Guid SenderUserId,
    string Text,
    DateTime SentAtUtc);
