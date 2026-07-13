using Malayisha.Domain.Enums;

namespace Malayisha.Domain.Entities;

public sealed class Booking
{
    private Booking() { }

    private Booking(
        Guid id,
        Guid tripListingId,
        Guid senderId,
        Guid transporterId,
        DateTime createdAtUtc,
        string? message)
    {
        Id = id;
        TripListingId = tripListingId;
        SenderId = senderId;
        TransporterId = transporterId;
        Message = message;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid TripListingId { get; private set; }
    public Guid SenderId { get; private set; }
    public Guid TransporterId { get; private set; }
    public BookingStatus Status { get; private set; } = BookingStatus.Requested;
    public decimal? QuotedPriceZar { get; private set; }
    public decimal? AgreedPriceZar { get; private set; }
    public string? Message { get; private set; }
    public DateTime? InTransitAtUtc { get; private set; }
    public DateTime? DeliveredAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public Guid? CancelledByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Booking Create(
        Guid id,
        Guid tripListingId,
        Guid senderId,
        Guid transporterId,
        DateTime nowUtc,
        string? message = null) =>
        new(id, tripListingId, senderId, transporterId, nowUtc, message);
}
