namespace Malayisha.Domain.Entities;

public sealed class ChatMessage
{
    private ChatMessage() { }

    private ChatMessage(Guid id, Guid bookingId, Guid senderUserId, string text, DateTime sentAtUtc)
    {
        Id = id;
        BookingId = bookingId;
        SenderUserId = senderUserId;
        Text = DomainGuard.Required(text, nameof(text));
        SentAtUtc = sentAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime SentAtUtc { get; private set; }

    public static ChatMessage Create(Guid id, Guid bookingId, Guid senderUserId, string text, DateTime nowUtc) =>
        new(id, bookingId, senderUserId, text, nowUtc);
}
