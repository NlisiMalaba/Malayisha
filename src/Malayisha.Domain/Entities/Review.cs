namespace Malayisha.Domain.Entities;

public sealed class Review
{
    private Review() { }

    private Review(
        Guid id,
        Guid bookingId,
        Guid senderId,
        Guid transporterProfileId,
        int rating,
        string? comment,
        DateTime createdAtUtc)
    {
        Id = id;
        BookingId = bookingId;
        SenderId = senderId;
        TransporterProfileId = transporterProfileId;
        Rating = DomainGuard.InRange(rating, 1, 5, nameof(rating));
        Comment = comment;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    public Guid SenderId { get; private set; }
    public Guid TransporterProfileId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public bool IsHidden { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Review Create(
        Guid id,
        Guid bookingId,
        Guid senderId,
        Guid transporterProfileId,
        int rating,
        string? comment,
        DateTime nowUtc) =>
        new(id, bookingId, senderId, transporterProfileId, rating, comment, nowUtc);

    public void SetVisibility(bool isHidden, DateTime nowUtc)
    {
        IsHidden = isHidden;
        UpdatedAtUtc = nowUtc;
    }
}
