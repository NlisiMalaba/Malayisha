namespace Malayisha.Api.Contracts.Chat;

public sealed record ChatMessageDto(
    Guid Id,
    Guid BookingId,
    Guid SenderUserId,
    string Text,
    DateTime SentAtUtc);

public sealed record ChatMessageHistoryResponse(IReadOnlyList<ChatMessageDto> Messages);
