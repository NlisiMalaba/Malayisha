using FsCheck;
using FsCheck.Xunit;
using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Tests;

public sealed class BookingStateMachinePropertyTests
{
    private static readonly Guid SenderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TransporterId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid OtherUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTime BaselineUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Property(MaxTest = 120)]
    public bool ValidTransitions_ReachExpectedStateAndMetadata(BookingStatus sourceStatus)
    {
        var booking = CreateBookingAtStatus(sourceStatus);
        var nowUtc = BaselineUtc.AddMinutes(10);
        var expected = ExpectedValidTransitionFor(sourceStatus);

        if (expected is null)
        {
            return true;
        }

        var result = booking.Transition(
            expected.Value.Target,
            expected.Value.ActorId,
            expected.Value.ActorRole,
            nowUtc,
            expected.Value.Amount,
            expected.Value.IsSystemAction);

        if (result.IsError || booking.Status != expected.Value.Target)
        {
            return false;
        }

        return expected.Value.Target switch
        {
            BookingStatus.Quoted => booking.QuotedPriceZar == expected.Value.Amount && booking.UpdatedAtUtc == nowUtc,
            BookingStatus.Confirmed => booking.AgreedPriceZar == expected.Value.Amount && booking.UpdatedAtUtc == nowUtc,
            BookingStatus.InTransit => booking.InTransitAtUtc == nowUtc && booking.UpdatedAtUtc == nowUtc,
            BookingStatus.Delivered => booking.DeliveredAtUtc == nowUtc && booking.UpdatedAtUtc == nowUtc,
            BookingStatus.Completed => booking.CompletedAtUtc == nowUtc && booking.UpdatedAtUtc == nowUtc,
            BookingStatus.Cancelled => booking.CancelledByUserId == expected.Value.ActorId
                                      && booking.CancelledAtUtc == nowUtc
                                      && booking.UpdatedAtUtc == nowUtc,
            _ => false
        };
    }

    [Property(MaxTest = 150)]
    public bool InvalidTransitions_ReturnErrorAndKeepState(
        BookingStatus sourceStatus,
        ArbitraryRole actorRole,
        BookingStatus targetStatus,
        decimal rawAmount)
    {
        var booking = CreateBookingAtStatus(sourceStatus);
        var actorId = ActorIdForRole(actorRole);
        var amount = NormalizeAmount(rawAmount);
        var nowUtc = BaselineUtc.AddHours(6);
        var snapshot = BookingSnapshot.From(booking);
        var isSystemAction = sourceStatus == BookingStatus.Delivered
                             && targetStatus == BookingStatus.Completed
                             && actorRole == ArbitraryRole.Admin;

        var shouldBeValid = IsValidTransition(sourceStatus, targetStatus, actorRole, actorId, amount, isSystemAction);

        if (shouldBeValid)
        {
            return true;
        }

        var result = booking.Transition(targetStatus, actorId, ToUserRole(actorRole), nowUtc, amount, isSystemAction);
        var current = BookingSnapshot.From(booking);

        return
            result.IsError
            && result.ErrorCode == "InvalidStateTransition"
            && snapshot.Equals(current);
    }

    private static Booking CreateBookingAtStatus(BookingStatus status)
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), SenderId, TransporterId, BaselineUtc, "initial");
        var stepTime = BaselineUtc;

        switch (status)
        {
            case BookingStatus.Requested:
                break;
            case BookingStatus.Quoted:
                _ = booking.Transition(BookingStatus.Quoted, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1), 700m);
                break;
            case BookingStatus.Confirmed:
                _ = booking.Transition(BookingStatus.Quoted, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1), 700m);
                _ = booking.Transition(BookingStatus.Confirmed, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1), 750m);
                break;
            case BookingStatus.InTransit:
                _ = booking.Transition(BookingStatus.Quoted, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1), 700m);
                _ = booking.Transition(BookingStatus.Confirmed, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1), 750m);
                _ = booking.Transition(BookingStatus.InTransit, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1));
                break;
            case BookingStatus.Delivered:
                _ = booking.Transition(BookingStatus.Quoted, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1), 700m);
                _ = booking.Transition(BookingStatus.Confirmed, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1), 750m);
                _ = booking.Transition(BookingStatus.InTransit, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1));
                _ = booking.Transition(BookingStatus.Delivered, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1));
                break;
            case BookingStatus.Completed:
                _ = booking.Transition(BookingStatus.Quoted, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1), 700m);
                _ = booking.Transition(BookingStatus.Confirmed, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1), 750m);
                _ = booking.Transition(BookingStatus.InTransit, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1));
                _ = booking.Transition(BookingStatus.Delivered, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1));
                _ = booking.Transition(BookingStatus.Completed, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1));
                break;
            case BookingStatus.Cancelled:
                _ = booking.Transition(BookingStatus.Cancelled, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }

        return booking;
    }

    private static (BookingStatus Target, Guid ActorId, UserRole ActorRole, decimal? Amount, bool IsSystemAction)? ExpectedValidTransitionFor(BookingStatus sourceStatus) =>
        sourceStatus switch
        {
            BookingStatus.Requested => (BookingStatus.Quoted, TransporterId, UserRole.Transporter, 500m, false),
            BookingStatus.Quoted => (BookingStatus.Confirmed, SenderId, UserRole.Sender, 550m, false),
            BookingStatus.Confirmed => (BookingStatus.InTransit, TransporterId, UserRole.Transporter, null, false),
            BookingStatus.InTransit => (BookingStatus.Delivered, TransporterId, UserRole.Transporter, null, false),
            BookingStatus.Delivered => (BookingStatus.Completed, SenderId, UserRole.Sender, null, false),
            _ => null
        };

    private static bool IsValidTransition(
        BookingStatus sourceStatus,
        BookingStatus targetStatus,
        ArbitraryRole actorRole,
        Guid actorId,
        decimal? amountZar,
        bool isSystemAction)
    {
        var isSender = actorRole == ArbitraryRole.Sender && actorId == SenderId;
        var isTransporter = actorRole == ArbitraryRole.Transporter && actorId == TransporterId;
        var isParticipant = isSender || isTransporter;

        return sourceStatus switch
        {
            BookingStatus.Requested => (targetStatus == BookingStatus.Quoted && isTransporter && amountZar is > 0)
                                       || (targetStatus == BookingStatus.Cancelled && isParticipant),
            BookingStatus.Quoted => (targetStatus == BookingStatus.Confirmed && isSender && amountZar is > 0)
                                    || (targetStatus == BookingStatus.Cancelled && isParticipant),
            BookingStatus.Confirmed => (targetStatus == BookingStatus.InTransit && isTransporter)
                                       || (targetStatus == BookingStatus.Cancelled && isParticipant),
            BookingStatus.InTransit => targetStatus == BookingStatus.Delivered && isTransporter,
            BookingStatus.Delivered => targetStatus == BookingStatus.Completed && (isSender || isSystemAction),
            _ => false
        };
    }

    private static Guid ActorIdForRole(ArbitraryRole role) =>
        role switch
        {
            ArbitraryRole.Sender => SenderId,
            ArbitraryRole.Transporter => TransporterId,
            ArbitraryRole.Admin => OtherUserId,
            _ => OtherUserId
        };

    private static UserRole ToUserRole(ArbitraryRole role) =>
        role switch
        {
            ArbitraryRole.Sender => UserRole.Sender,
            ArbitraryRole.Transporter => UserRole.Transporter,
            ArbitraryRole.Admin => UserRole.Admin,
            _ => UserRole.Admin
        };

    private static decimal? NormalizeAmount(decimal value)
    {
        var rounded = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        return rounded;
    }

    private readonly record struct BookingSnapshot(
        BookingStatus Status,
        decimal? QuotedPriceZar,
        decimal? AgreedPriceZar,
        DateTime? InTransitAtUtc,
        DateTime? DeliveredAtUtc,
        DateTime? CompletedAtUtc,
        Guid? CancelledByUserId,
        DateTime? CancelledAtUtc,
        DateTime UpdatedAtUtc)
    {
        public static BookingSnapshot From(Booking booking) =>
            new(
                booking.Status,
                booking.QuotedPriceZar,
                booking.AgreedPriceZar,
                booking.InTransitAtUtc,
                booking.DeliveredAtUtc,
                booking.CompletedAtUtc,
                booking.CancelledByUserId,
                booking.CancelledAtUtc,
                booking.UpdatedAtUtc);
    }
    public enum ArbitraryRole
    {
        Sender = 1,
        Transporter = 2,
        Admin = 3
    }
}
