using Malayisha.Domain.Entities;
using Malayisha.Domain.Enums;

namespace Malayisha.Application.Tests;

public sealed class BookingStateMachineEdgeCaseTests
{
    private static readonly Guid SenderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid TransporterId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherUserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTime CreatedAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TransitionAtUtc = new(2026, 1, 1, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Requested_ToQuoted_WithOwningTransporter_Succeeds()
    {
        var booking = CreateBookingAtStatus(BookingStatus.Requested);

        var result = booking.Transition(BookingStatus.Quoted, TransporterId, UserRole.Transporter, TransitionAtUtc, 500m);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Quoted, booking.Status);
        Assert.Equal(500m, booking.QuotedPriceZar);
        Assert.Equal(TransitionAtUtc, booking.UpdatedAtUtc);
    }

    [Fact]
    public void Quoted_ToConfirmed_WithOwningSender_Succeeds()
    {
        var booking = CreateBookingAtStatus(BookingStatus.Quoted);

        var result = booking.Transition(BookingStatus.Confirmed, SenderId, UserRole.Sender, TransitionAtUtc, 550m);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(550m, booking.AgreedPriceZar);
        Assert.Equal(TransitionAtUtc, booking.UpdatedAtUtc);
    }

    [Fact]
    public void Confirmed_ToInTransit_WithOwningTransporter_Succeeds()
    {
        var booking = CreateBookingAtStatus(BookingStatus.Confirmed);

        var result = booking.Transition(BookingStatus.InTransit, TransporterId, UserRole.Transporter, TransitionAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.InTransit, booking.Status);
        Assert.Equal(TransitionAtUtc, booking.InTransitAtUtc);
        Assert.Equal(TransitionAtUtc, booking.UpdatedAtUtc);
    }

    [Fact]
    public void InTransit_ToDelivered_WithOwningTransporter_Succeeds()
    {
        var booking = CreateBookingAtStatus(BookingStatus.InTransit);

        var result = booking.Transition(BookingStatus.Delivered, TransporterId, UserRole.Transporter, TransitionAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Delivered, booking.Status);
        Assert.Equal(TransitionAtUtc, booking.DeliveredAtUtc);
        Assert.Equal(TransitionAtUtc, booking.UpdatedAtUtc);
    }

    [Fact]
    public void Delivered_ToCompleted_WithOwningSender_Succeeds()
    {
        var booking = CreateBookingAtStatus(BookingStatus.Delivered);

        var result = booking.Transition(BookingStatus.Completed, SenderId, UserRole.Sender, TransitionAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(TransitionAtUtc, booking.CompletedAtUtc);
        Assert.Equal(TransitionAtUtc, booking.UpdatedAtUtc);
    }

    [Fact]
    public void Delivered_ToCompleted_WithSystemAction_Succeeds()
    {
        var booking = CreateBookingAtStatus(BookingStatus.Delivered);

        var result = booking.Transition(
            BookingStatus.Completed,
            OtherUserId,
            UserRole.Admin,
            TransitionAtUtc,
            isSystemAction: true);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Equal(TransitionAtUtc, booking.CompletedAtUtc);
        Assert.Equal(TransitionAtUtc, booking.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(BookingStatus.Requested, UserRole.Sender)]
    [InlineData(BookingStatus.Requested, UserRole.Transporter)]
    [InlineData(BookingStatus.Quoted, UserRole.Sender)]
    [InlineData(BookingStatus.Quoted, UserRole.Transporter)]
    [InlineData(BookingStatus.Confirmed, UserRole.Sender)]
    [InlineData(BookingStatus.Confirmed, UserRole.Transporter)]
    public void CancellableStatus_ToCancelled_WithParticipant_Succeeds(BookingStatus sourceStatus, UserRole actorRole)
    {
        var booking = CreateBookingAtStatus(sourceStatus);
        var actorId = actorRole == UserRole.Sender ? SenderId : TransporterId;

        var result = booking.Transition(BookingStatus.Cancelled, actorId, actorRole, TransitionAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        Assert.Equal(actorId, booking.CancelledByUserId);
        Assert.Equal(TransitionAtUtc, booking.CancelledAtUtc);
        Assert.Equal(TransitionAtUtc, booking.UpdatedAtUtc);
    }

    [Theory]
    [InlineData(BookingStatus.Requested, BookingStatus.Confirmed, UserRole.Sender)]
    [InlineData(BookingStatus.Quoted, BookingStatus.InTransit, UserRole.Transporter)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Delivered, UserRole.Transporter)]
    [InlineData(BookingStatus.InTransit, BookingStatus.Completed, UserRole.Transporter)]
    [InlineData(BookingStatus.Delivered, BookingStatus.Cancelled, UserRole.Sender)]
    [InlineData(BookingStatus.Completed, BookingStatus.Cancelled, UserRole.Sender)]
    [InlineData(BookingStatus.Cancelled, BookingStatus.Quoted, UserRole.Transporter)]
    public void Transition_WithWrongSourceStatus_ReturnsErrorAndLeavesBookingUnchanged(
        BookingStatus sourceStatus,
        BookingStatus targetStatus,
        UserRole actorRole)
    {
        var booking = CreateBookingAtStatus(sourceStatus);
        var snapshot = BookingSnapshot.From(booking);
        var actorId = actorRole == UserRole.Sender ? SenderId : TransporterId;

        var result = booking.Transition(targetStatus, actorId, actorRole, TransitionAtUtc, 500m);

        AssertInvalidStateTransition(result);
        Assert.Equal(snapshot, BookingSnapshot.From(booking));
    }

    [Theory]
    [InlineData(BookingStatus.Requested, BookingStatus.Quoted, UserRole.Sender)]
    [InlineData(BookingStatus.Quoted, BookingStatus.Confirmed, UserRole.Transporter)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.InTransit, UserRole.Sender)]
    [InlineData(BookingStatus.InTransit, BookingStatus.Delivered, UserRole.Sender)]
    [InlineData(BookingStatus.Delivered, BookingStatus.Completed, UserRole.Transporter)]
    public void Transition_WithWrongRole_ReturnsErrorAndLeavesBookingUnchanged(
        BookingStatus sourceStatus,
        BookingStatus targetStatus,
        UserRole wrongRole)
    {
        var booking = CreateBookingAtStatus(sourceStatus);
        var snapshot = BookingSnapshot.From(booking);
        var actorId = wrongRole == UserRole.Sender ? SenderId : TransporterId;

        var result = booking.Transition(targetStatus, actorId, wrongRole, TransitionAtUtc, 500m);

        AssertInvalidStateTransition(result);
        Assert.Equal(snapshot, BookingSnapshot.From(booking));
    }

    [Theory]
    [InlineData(BookingStatus.Requested, BookingStatus.Quoted, UserRole.Transporter)]
    [InlineData(BookingStatus.Quoted, BookingStatus.Confirmed, UserRole.Sender)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.InTransit, UserRole.Transporter)]
    [InlineData(BookingStatus.InTransit, BookingStatus.Delivered, UserRole.Transporter)]
    [InlineData(BookingStatus.Delivered, BookingStatus.Completed, UserRole.Sender)]
    [InlineData(BookingStatus.Requested, BookingStatus.Cancelled, UserRole.Sender)]
    [InlineData(BookingStatus.Quoted, BookingStatus.Cancelled, UserRole.Transporter)]
    [InlineData(BookingStatus.Confirmed, BookingStatus.Cancelled, UserRole.Sender)]
    public void Transition_WithWrongActorId_ReturnsErrorAndLeavesBookingUnchanged(
        BookingStatus sourceStatus,
        BookingStatus targetStatus,
        UserRole actorRole)
    {
        var booking = CreateBookingAtStatus(sourceStatus);
        var snapshot = BookingSnapshot.From(booking);

        var result = booking.Transition(targetStatus, OtherUserId, actorRole, TransitionAtUtc, 500m);

        AssertInvalidStateTransition(result);
        Assert.Equal(snapshot, BookingSnapshot.From(booking));
    }

    private static Booking CreateBookingAtStatus(BookingStatus status)
    {
        var booking = Booking.Create(Guid.NewGuid(), Guid.NewGuid(), SenderId, TransporterId, CreatedAtUtc);
        var stepTime = CreatedAtUtc;

        if (status == BookingStatus.Requested)
        {
            return booking;
        }

        _ = booking.Transition(BookingStatus.Quoted, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1), 700m);

        if (status == BookingStatus.Quoted)
        {
            return booking;
        }

        _ = booking.Transition(BookingStatus.Confirmed, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1), 750m);

        if (status == BookingStatus.Confirmed)
        {
            return booking;
        }

        if (status == BookingStatus.Cancelled)
        {
            _ = booking.Transition(BookingStatus.Cancelled, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1));
            return booking;
        }

        _ = booking.Transition(BookingStatus.InTransit, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1));

        if (status == BookingStatus.InTransit)
        {
            return booking;
        }

        _ = booking.Transition(BookingStatus.Delivered, TransporterId, UserRole.Transporter, stepTime = stepTime.AddMinutes(1));

        if (status == BookingStatus.Delivered)
        {
            return booking;
        }

        _ = booking.Transition(BookingStatus.Completed, SenderId, UserRole.Sender, stepTime = stepTime.AddMinutes(1));
        return booking;
    }

    private static void AssertInvalidStateTransition(Malayisha.Domain.Common.Result result)
    {
        Assert.True(result.IsError);
        Assert.Equal("InvalidStateTransition", result.ErrorCode);
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
}
