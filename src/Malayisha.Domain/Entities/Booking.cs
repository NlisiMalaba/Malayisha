using Malayisha.Domain.Common;
using Malayisha.Domain.Enums;

namespace Malayisha.Domain.Entities;

public sealed class Booking
{
    private const string InvalidStateTransitionError = "InvalidStateTransition";

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

    public Result Transition(
        BookingStatus targetStatus,
        Guid actorId,
        UserRole actorRole,
        DateTime nowUtc,
        decimal? amountZar = null,
        bool isSystemAction = false)
    {
        if (Status == BookingStatus.Requested &&
            targetStatus == BookingStatus.Quoted &&
            IsTransporterAction(actorId, actorRole))
        {
            if (!amountZar.HasValue || amountZar.Value <= 0)
            {
                return Result.Error(InvalidStateTransitionError);
            }

            QuotedPriceZar = amountZar.Value;
            Status = BookingStatus.Quoted;
            UpdatedAtUtc = nowUtc;
            return Result.Success();
        }

        if (Status == BookingStatus.Quoted &&
            targetStatus == BookingStatus.Confirmed &&
            IsSenderAction(actorId, actorRole))
        {
            if (!amountZar.HasValue || amountZar.Value <= 0)
            {
                return Result.Error(InvalidStateTransitionError);
            }

            AgreedPriceZar = amountZar.Value;
            Status = BookingStatus.Confirmed;
            UpdatedAtUtc = nowUtc;
            return Result.Success();
        }

        if (Status == BookingStatus.Confirmed &&
            targetStatus == BookingStatus.InTransit &&
            IsTransporterAction(actorId, actorRole))
        {
            InTransitAtUtc = nowUtc;
            Status = BookingStatus.InTransit;
            UpdatedAtUtc = nowUtc;
            return Result.Success();
        }

        if (Status == BookingStatus.InTransit &&
            targetStatus == BookingStatus.Delivered &&
            IsTransporterAction(actorId, actorRole))
        {
            DeliveredAtUtc = nowUtc;
            Status = BookingStatus.Delivered;
            UpdatedAtUtc = nowUtc;
            return Result.Success();
        }

        if (Status == BookingStatus.Delivered &&
            targetStatus == BookingStatus.Completed &&
            (IsSenderAction(actorId, actorRole) || isSystemAction))
        {
            CompletedAtUtc = nowUtc;
            Status = BookingStatus.Completed;
            UpdatedAtUtc = nowUtc;
            return Result.Success();
        }

        if (CanCancelFromCurrentStatus() &&
            targetStatus == BookingStatus.Cancelled &&
            IsParticipantAction(actorId, actorRole))
        {
            CancelledByUserId = actorId;
            CancelledAtUtc = nowUtc;
            Status = BookingStatus.Cancelled;
            UpdatedAtUtc = nowUtc;
            return Result.Success();
        }

        return Result.Error(InvalidStateTransitionError);
    }

    private bool CanCancelFromCurrentStatus() =>
        Status == BookingStatus.Requested ||
        Status == BookingStatus.Quoted ||
        Status == BookingStatus.Confirmed;

    private bool IsSenderAction(Guid actorId, UserRole actorRole) =>
        actorRole == UserRole.Sender && actorId == SenderId;

    private bool IsTransporterAction(Guid actorId, UserRole actorRole) =>
        actorRole == UserRole.Transporter && actorId == TransporterId;

    private bool IsParticipantAction(Guid actorId, UserRole actorRole) =>
        IsSenderAction(actorId, actorRole) || IsTransporterAction(actorId, actorRole);
}
